# MiniShop

Интернет-магазин на ASP.NET Core Web API с клиентом на JavaScript.

ASP.NET Core 10, Entity Framework Core + SQLite, JWT, Swagger.

## Запуск

```bash
dotnet run
```

- Сайт: http://localhost:5248
- Swagger: http://localhost:5248/swagger

База `minishop.db` создаётся при первом запуске вместе с тестовыми данными.

## Тестовые аккаунты

| Email | Пароль | Роль |
|---|---|---|
| admin@shop.local | Admin123 | Admin |
| user@shop.local | User123 | User |
| maria@shop.local | Maria123 | User |

## Функционал

- каталог с поиском, фильтрами, сортировкой и пагинацией
- корзина и оформление заказов, отмена заказа
- отзывы и рейтинг (только на полученные товары)
- админ-панель: товары, категории, заказы и смена статусов, модерация отзывов
- загрузка картинок товаров
