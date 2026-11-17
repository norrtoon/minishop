using MiniShop.DTOs;
using MiniShop.Exceptions;
using MiniShop.Models;
using MiniShop.Repositories;
using MiniShop.Security;

namespace MiniShop.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<UserDto> GetProfileAsync(int userId);
}

public class AuthService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await users.EmailExistsAsync(email))
            throw new ConflictException("Пользователь с таким email уже зарегистрирован.");

        var user = new User
        {
            Email = email,
            Name = dto.Name.Trim(),
            PasswordHash = passwordHasher.Hash(dto.Password),
            Role = Roles.User
        };

        await users.AddAsync(user);
        await unitOfWork.SaveChangesAsync();

        return BuildResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await users.GetByEmailAsync(dto.Email.Trim());

        if (user is null || !passwordHasher.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedException("Неверный email или пароль.");

        return BuildResponse(user);
    }

    public async Task<UserDto> GetProfileAsync(int userId)
    {
        var user = await users.GetByIdAsync(userId)
                   ?? throw new NotFoundException("Пользователь", userId);
        return user.ToDto();
    }

    private AuthResponseDto BuildResponse(User user)
    {
        var (token, expiresAt) = tokenService.CreateToken(user);
        return new AuthResponseDto { Token = token, ExpiresAt = expiresAt, User = user.ToDto() };
    }
}
