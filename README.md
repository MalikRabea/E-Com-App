# 🛍️ Shop E-Commerce Backend

This is the backend API for the **Shop E-Commerce Project**, built with **ASP.NET Core (.NET 8)** following **Clean Architecture** principles.  
It provides APIs for products, categories, basket, favorites, orders, checkout, and payment using **Stripe**.

---

## 🚀 Tech Stack
- **ASP.NET Core (.NET 8)** – Web API
- **Entity Framework Core** – ORM for SQL Server
- **SQL Server** – Relational database
- **Redis** – Basket caching
- **Stripe** – Payment gateway
- **Clean Architecture** – API, Core, Infrastructure layers
- **Repository & Unit of Work Pattern**
- **DTOs & Services** – For clean data transfer and business logic
- **Mailtrap** – Email testing in development

---

## ⚡ Features
- 🔑 **Authentication & Authorization** (JWT)
- 🛒 Basket with **Redis**
- ❤️ Favorites management
- 📦 Orders system (Checkout → Address → Delivery → Payment)
- 💳 Stripe integration for secure payments
- 📧 Email confirmation using Mailtrap
- ⭐ Product rating after purchase
- 📊 Clean separation of concerns (API / Core / Infrastructure)

---

## 🛠️ Getting Started

### 1️⃣ Clone the repository
```bash
git clone https://github.com/YOUR_USERNAME/shop-ecommerce-backend.git
cd shop-ecommerce-backend
