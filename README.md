# Hệ thống Quản lý Thư viện UNETI

> Đồ án nhóm gồm 5 thành viên, xây dựng ứng dụng web hỗ trợ bạn đọc tra cứu sách và theo dõi việc mượn sách, đồng thời giúp thư viện quản lý bạn đọc, sách, phiếu mượn, trả sách và các khoản phạt trên cùng một hệ thống.

## Mục lục

- [Chức năng](#chức-năng)
- [Cài đặt và chạy ứng dụng](#cài-đặt-và-chạy-ứng-dụng)
- [Cách sử dụng](#cách-sử-dụng)
- [Dữ liệu mẫu](#dữ-liệu-mẫu)
- [Đóng góp](#đóng-góp)
- [Giấy phép](#giấy-phép)

## Chức năng

- **Dành cho bạn đọc:** đăng ký tài khoản, tra cứu sách, xem thông tin sách, gửi yêu cầu mượn, theo dõi phiếu mượn và lịch sử, yêu cầu gia hạn, xem thông báo và các khoản phạt.
- **Dành cho nhân viên thư viện:** quản lý sách và thể loại, hồ sơ bạn đọc, yêu cầu mượn, gia hạn, trả sách, tiền phạt, thông báo và cấu hình thư viện.
- **Thanh toán:** ghi nhận thanh toán khoản phạt; hỗ trợ tích hợp payOS.
- **Theo dõi hoạt động:** cung cấp lịch sử thanh toán và thống kê phục vụ công tác quản lý.

## Cài đặt và chạy ứng dụng

### Yêu cầu

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Microsoft SQL Server

### Các bước

1. Sao chép mã nguồn và mở thư mục dự án:

   ```bash
   git clone <đường-dẫn-repository>
   cd QuanLyThuVien_UNETI4_TI17A1CL
   ```

2. Cấu hình chuỗi kết nối SQL Server bằng User Secrets:

   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<chuỗi-kết-nối-SQL-Server-của-bạn>"
   ```

   Tài khoản SQL Server cần có quyền tạo và cập nhật cơ sở dữ liệu. Không đưa mật khẩu hoặc khóa dịch vụ vào mã nguồn.

3. (Tùy chọn) Cấu hình SMTP để thử các chức năng gửi email và OTP. Dùng thông tin SMTP của nhà cung cấp email; với Gmail, hãy tạo **App Password** và dùng mật khẩu đó thay cho mật khẩu đăng nhập thông thường:

   ```bash
   dotnet user-secrets set "Email:Host" "<smtp-host>"
   dotnet user-secrets set "Email:Port" "587"
   dotnet user-secrets set "Email:UseStartTls" "true"
   dotnet user-secrets set "Email:Username" "<địa-chỉ-email>"
   dotnet user-secrets set "Email:Password" "<app-password-hoặc-mật-khẩu-smtp>"
   dotnet user-secrets set "Email:FromAddress" "<địa-chỉ-email-gửi>"
   dotnet user-secrets set "Email:FromName" "Thư viện UNETI"
   ```

4. (Tùy chọn) Để thử thanh toán, cấu hình thông tin API payOS. Dùng thông tin sandbox/test nếu payOS cung cấp cho tài khoản của bạn:

   ```bash
   dotnet user-secrets set "PayOS:ClientId" "<client-id>"
   dotnet user-secrets set "PayOS:ApiKey" "<api-key>"
   dotnet user-secrets set "PayOS:ChecksumKey" "<checksum-key>"
   dotnet user-secrets set "PayOS:PublicBaseUrl" "<địa-chỉ-công-khai-của-ứng-dụng>"
   ```

   Chức năng thanh toán cần URL công khai phù hợp để payOS có thể chuyển hướng hoặc gửi webhook khi cần. Không chia sẻ hay commit các khóa API.

5. Khôi phục các gói và chạy ứng dụng:

   ```bash
   dotnet restore
   dotnet run
   ```

   Khi khởi động, ứng dụng áp dụng các migration đã có và khởi tạo dữ liệu cần thiết trong cơ sở dữ liệu.

6. Mở địa chỉ cục bộ được in trong cửa sổ lệnh bằng trình duyệt.

## Cách sử dụng

Bạn đọc có thể xem danh mục sách và đăng ký tài khoản mới. Sau khi đăng nhập, các chức năng được hiển thị theo vai trò tài khoản. Nhân viên thư viện sử dụng các màn hình quản lý để xử lý sách, bạn đọc, mượn trả và khoản phạt.

Chạy lại ứng dụng bằng lệnh:

```bash
dotnet run
```

## Dữ liệu mẫu

Dự án có dữ liệu mẫu được khởi tạo trong `Data/DbSeeder.cs` để hỗ trợ việc chạy và giới thiệu các chức năng của ứng dụng.

## Đóng góp

Dự án không tiếp nhận đóng góp bên ngoài.
