# Event Management System

A **.NET MVC Event Management System** for organizing and booking events, generating tickets with QR codes, handling loyalty points, and managing payments. This project is built with ASP.NET Core MVC, Entity Framework Core, and MySQL.

---

## **Features**

- **User Authentication**: Registration and login with role-based access (Customer / Organizer / Admin).  
- **Event Management**: Create, edit, and delete events with categories, venues, and images.  
- **Booking System**: Customers can book events, apply promotions, and use loyalty points.  
- **Ticket Generation**: Automatic ticket creation with unique ticket numbers and QR codes.  
- **Loyalty System**: Earn and redeem loyalty points for bookings.  
- **Payment Integration**: Record payments for bookings with multiple payment methods.  
- **Share & Download Tickets**: Share tickets via email, WhatsApp, SMS, or download as images.  
- **Responsive Design**: Works on desktop, tablet, and mobile devices.  

---

## **Project Structure**

EventManagementSystem/
├── EventManagementSystem.sln # Solution file
├── EventManagementSystem/ # MVC project
│ ├── Controllers/
│ ├── Models/
│ ├── Views/
│ ├── wwwroot/
│ └── appsettings.json # Database and configuration
├── README.md
└── .gitignore

---

## **Requirements**

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)  
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or newer  
- MySQL Server or MariaDB  
- Node.js (optional, if using frontend bundling tools)

---

## **Setup Instructions**

1. **Clone the repository**

```bash
git clone https://github.com/USERNAME/EventManagementSystem.git
cd EventManagementSystem

Open the solution
Open EventManagementSystem.sln in Visual Studio.

Restore NuGet packages
dotnet restore

Configure the database
Edit appsettings.json:

"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=EventDB;User Id=root;Password=YourPassword;"
}

Run the project
Press F5 in Visual Studio to launch the application in your browser.

Usage

Register as a customer or login as admin/organizer.

Browse events, book tickets, and manage bookings.

Download or share tickets via QR codes.

Admins can view all bookings, payments, and loyalty points.




