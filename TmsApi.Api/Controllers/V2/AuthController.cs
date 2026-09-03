using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Identity;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/v2/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<TmsUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly TmsDbContext _context;
    private readonly TokenService _tokenService;

    public AuthController(
        UserManager<TmsUser> userManager,
        RoleManager<IdentityRole> roleManager,
        TmsDbContext context,
        TokenService tokenService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _tokenService = tokenService;
    }

    // REGISTER
    // POST: /api/Auth/register

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
    {
        // Check whether email already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);

        if (existingUser != null)
        {
            return Conflict(new
            {
                detail = "A user with this email already exists."
            });
        }

        // Check whether username already exists
        var existingUsername = await _userManager.FindByNameAsync(request.Email);

        if (existingUsername != null)
        {
            return Conflict(new
            {
                detail = "A user with this username already exists."
            });
        }

        // Allowed roles
        var allowedRoles = new[]
        {
            "Student",
            "Instructor",
            "Admin"
        };

        // Validate role
        if (!allowedRoles.Contains(
                request.Role,
                StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                detail = "Invalid role. Allowed roles are Student, Instructor, and Admin."
            });
        }

        // Normalize role name
        var roleName = allowedRoles.First(
            r => r.Equals(
                request.Role,
                StringComparison.OrdinalIgnoreCase));

        // Create user
        var user = new TmsUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        // Create user with password
        var result = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                detail = result.Errors.Select(e => e.Description)
            });
        }

        // Create role if it doesn't exist
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var roleResult = await _roleManager.CreateAsync(
                new IdentityRole(roleName));

            if (!roleResult.Succeeded)
            {
                // Remove user if role creation failed
                await _userManager.DeleteAsync(user);

                return StatusCode(500, new
                {
                    detail = "Failed to create user role."
                });
            }
        }

        // Assign role to user
        var addRoleResult = await _userManager.AddToRoleAsync(
            user,
            roleName);

        if (!addRoleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            return StatusCode(500, new
            {
                detail = "Failed to assign user role."
            });
        }

        return Ok(new
        {
            message = "User registered successfully.",
            user = new
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                role = roleName
            }
        });
    }


    // LOGIN
    // POST: /api/Auth/login
    [EnableRateLimiting("AuthLimiter")]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        // Find user by username
        var user = await _userManager.FindByNameAsync(
            request.Email);

        if (user == null)
        {
            return Unauthorized(new
            {
                detail = "Invalid credentials."
            });
        }

        // Check if account is locked
        if (await _userManager.IsLockedOutAsync(user))
        {
            return StatusCode(423, new
            {
                detail = "Account locked due to multiple failed login attempts."
            });
        }

        // Check password
        var validPassword = await _userManager.CheckPasswordAsync(
            user,
            request.Password);

        if (!validPassword)
        {
            await _userManager.AccessFailedAsync(user);

            return Unauthorized(new
            {
                detail = "Invalid credentials."
            });
        }

        // Reset failed login attempts
        await _userManager.ResetAccessFailedCountAsync(user);

        // Get roles
        var roles = await _userManager.GetRolesAsync(user);

        // Generate access token
        var accessToken = _tokenService.GenerateJwt(
            user,
            roles);

        // Create refresh token
        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            accessToken,
            refreshToken = refreshToken.Token
        });
    }


    // REFRESH TOKEN
    // POST: /api/Auth/refresh

    public record RefreshRequest(string RefreshToken);

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request)
    {
        // Find refresh token
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(
                rt => rt.Token == request.RefreshToken);

        // Token doesn't exist
        if (storedToken == null)
        {
            return Unauthorized(new
            {
                detail = "Invalid refresh token."
            });
        }

        // =====================================================
        // REFRESH TOKEN THEFT DETECTION
        // =====================================================

        // If an already-used refresh token is submitted,
        // someone may have stolen an old token.
        if (storedToken.IsUsed)
        {
            // Revoke ALL refresh tokens belonging to this user
            var userTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == storedToken.UserId)
                .ToListAsync();

            foreach (var token in userTokens)
            {
                token.IsRevoked = true;
            }

            await _context.SaveChangesAsync();

            return Unauthorized(new
            {
                detail = "Token theft detected. All user sessions revoked."
            });
        }

        // =====================================================
        // CHECK TOKEN STATUS
        // =====================================================

        if (storedToken.IsRevoked)
        {
            return Unauthorized(new
            {
                detail = "Refresh token has been revoked."
            });
        }

        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized(new
            {
                detail = "Refresh token has expired."
            });
        }

        // =====================================================
        // ROTATE REFRESH TOKEN
        // =====================================================

        // Mark old token as used
        storedToken.IsUsed = true;

        // Create new refresh token
        var newRefreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = storedToken.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };

        _context.RefreshTokens.Add(newRefreshToken);

        // =====================================================
        // FIND USER
        // =====================================================

        var user = await _userManager.FindByIdAsync(
            storedToken.UserId);

        if (user == null)
        {
            return Unauthorized(new
            {
                detail = "User not found."
            });
        }

        // Get user's roles
        var roles = await _userManager.GetRolesAsync(user);

        // Generate new access token
        var newAccessToken = _tokenService.GenerateJwt(
            user,
            roles);

        await _context.SaveChangesAsync();

        // Return new token pair
        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken.Token
        });
    }
}