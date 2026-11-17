using System.ComponentModel.DataAnnotations;

namespace MiniShop.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Имя обязательно")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Имя должно быть от 2 до 100 символов")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть не короче 6 символов")]
    [RegularExpression(@"^(?=.*[A-Za-zА-Яа-я])(?=.*\d).+$",
        ErrorMessage = "Пароль должен содержать хотя бы одну букву и одну цифру")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите пароль")]
    [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
