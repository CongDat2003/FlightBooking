# Hướng dẫn dừng và khởi động lại API

## Bước 1: Dừng API hiện tại

Nếu API đang chạy, bạn cần dừng nó trước khi build:

### Cách 1: Dùng Task Manager
1. Mở Task Manager (Ctrl + Shift + Esc)
2. Tìm process `FlightBooking.exe` hoặc `FlightBooking (21448)`
3. Nhấn "End Task"

### Cách 2: Dùng Command Line (PowerShell)
```powershell
taskkill /F /IM FlightBooking.exe
```

### Cách 3: Dùng Command Line với PID
```powershell
taskkill /F /PID 21448
```

## Bước 2: Build lại API

```powershell
cd C:\Users\Admin\Downloads\PRM392_Flight-Booking-main\PRM392_Flight-Booking-main\API\FlightBooking
dotnet build
```

## Bước 3: Chạy lại API

```powershell
dotnet run
```

## Lưu ý

- Nếu bạn đang test API từ Postman hoặc browser, hãy đảm bảo đóng các request đang pending.
- Nếu có lỗi "file is locked", hãy kiểm tra Task Manager để tìm và kill process đang chạy.

