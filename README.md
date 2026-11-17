# Hệ thống Quản lý Thư viện UNETI

> Đồ án nhóm 5 thành viên: ứng dụng web giúp bạn đọc tra cứu sách, gửi yêu cầu mượn và gia hạn, theo dõi khoản phạt; đồng thời hỗ trợ nhân viên quản lý danh mục sách và các nghiệp vụ thư viện trên một hệ thống.

## Mục lục

- [Tổng quan](#tổng-quan)
- [Chức năng](#chức-năng)
- [Công nghệ](#công-nghệ)
- [Cài đặt và chạy ứng dụng](#cài-đặt-và-chạy-ứng-dụng)
- [Cách sử dụng](#cách-sử-dụng)
- [Dữ liệu mẫu](#dữ-liệu-mẫu)
- [Đóng góp](#đóng-góp)
- [Giấy phép](#giấy-phép)

## Tổng quan

Hệ thống phục vụ bạn đọc, nhân viên thư viện và quản trị viên, với giao diện và nghiệp vụ bằng tiếng Việt. README này dành cho người mới tìm hiểu dự án hoặc nhà tuyển dụng muốn nắm nhanh mục đích và khả năng của ứng dụng.

## Chức năng

- **Bạn đọc:** đăng ký và quản lý hồ sơ/thẻ; tìm kiếm, lọc và xem thông tin sách; gửi hoặc hủy yêu cầu mượn; theo dõi phiếu mượn; yêu cầu gia hạn; xem thông báo, tiền phạt và lịch sử thanh toán.
- **Nhân viên thư viện:** quản lý sách, thể loại và hồ sơ bạn đọc; xử lý yêu cầu cấp thẻ, mượn và gia hạn; giao/nhận sách; tính và thu phạt tại quầy; gửi nhắc trả sách, gia hạn hoặc nộp phạt; quản lý thông báo và xem thống kê.
- **Quản trị viên:** sử dụng nghiệp vụ thư viện và quản lý tài khoản, cấu hình thư viện.
- **Thanh toán trực tuyến:** tích hợp payOS để thanh toán tiền phạt. Có thể chạy các chức năng còn lại mà chưa cấu hình payOS.
- **Thông báo và email:** thông báo trong ứng dụng, tùy chọn nhận thông báo, email OTP đặt lại mật khẩu và email thông báo được gửi qua hàng đợi.
- **Tự động xử lý:** phiếu đã duyệt nhưng chưa được nhận sẽ tự hủy sau 7 ngày; bạn đọc được thông báo sau khi phiếu được xử lý.

## Công nghệ

- .NET 10, ASP.NET Core MVC, Razor.
- Entity Framework Core 10 và SQL Server.
- Bootstrap 5, jQuery, CSS và JavaScript.
- MailKit cho SMTP; payOS SDK cho thanh toán trực tuyến.

## Cài đặt và chạy ứng dụng

### Yêu cầu

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server có thể truy cập từ máy chạy ứng dụng.

### 1. Lấy mã nguồn

```bash
git clone <đường-dẫn-repository>
cd QuanLyThuVien_UNETI4_TI17A1CL
```

Chạy các lệnh tiếp theo tại thư mục chứa `QuanLyThuVien_UNETI4_TI17A1CL.csproj`.

### 2. Cấu hình SQL Server

Dự án đã có `UserSecretsId`. Đặt chuỗi kết nối riêng cho máy của bạn bằng User Secrets hoặc cấu hình môi trường:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<chuỗi-kết-nối-SQL-Server-của-bạn>"
```

Tài khoản SQL Server cần quyền tạo và cập nhật cơ sở dữ liệu. Không đưa mật khẩu hoặc khóa dịch vụ vào mã nguồn.

### 3. Cấu hình email (tùy chọn)

SMTP cần thiết để thử gửi email thật cho OTP và thông báo. Với Gmail, hãy tạo **App Password** và dùng mật khẩu ứng dụng thay cho mật khẩu đăng nhập:

```bash
dotnet user-secrets set "Email:Host" "smtp.gmail.com"
dotnet user-secrets set "Email:Port" "587"
dotnet user-secrets set "Email:UseStartTls" "true"
dotnet user-secrets set "Email:Username" "<địa-chỉ-email>"
dotnet user-secrets set "Email:Password" "<app-password>"
dotnet user-secrets set "Email:FromAddress" "<địa-chỉ-email-gửi>"
dotnet user-secrets set "Email:FromName" "Thư viện UNETI"
```

Trong môi trường Development, nếu chưa cấu hình SMTP, ứng dụng có thể ghi email OTP vào log để hỗ trợ phát triển. Khi SMTP được cấu hình trong Development, ứng dụng vừa gửi vừa ghi log. Ngoài Development, cần cấu hình `Email:Host` để ứng dụng khởi động.

### 4. Cấu hình payOS (tùy chọn)

Chỉ cần cấu hình nếu muốn thử thanh toán trực tuyến. Dùng thông tin API phù hợp với tài khoản của bạn:

```bash
dotnet user-secrets set "PayOS:ClientId" "<client-id>"
dotnet user-secrets set "PayOS:ApiKey" "<api-key>"
dotnet user-secrets set "PayOS:ChecksumKey" "<checksum-key>"
dotnet user-secrets set "PayOS:PublicBaseUrl" "<địa-chỉ-công-khai-của-ứng-dụng>"
```

URL công khai cần phù hợp để payOS chuyển hướng hoặc gửi webhook. Không chia sẻ hay commit các khóa API.

### 5. Khôi phục gói và chạy

```bash
dotnet restore
dotnet run
```

Khi khởi động, ứng dụng áp dụng EF migrations và gọi seeder. `Seed:SampleData` hiện được đặt thành `true` trong `appsettings.json`. Để bỏ qua các bộ dữ liệu mẫu lớn trên máy hiện tại, đặt:

```bash
dotnet user-secrets set "Seed:SampleData" "false"
```

Dữ liệu nền như cấu hình thư viện và tài khoản mặc định vẫn được seeder tạo nếu chưa có.

Mở địa chỉ cục bộ được in trong cửa sổ lệnh bằng trình duyệt.

## Cách sử dụng

Khách có thể xem trang chủ và danh mục sách, sau đó đăng ký tài khoản bạn đọc. Sau khi đăng nhập, chức năng hiển thị tùy theo vai trò và trạng thái thẻ. Nhân viên thư viện và quản trị viên sử dụng các màn hình nghiệp vụ tương ứng để xử lý sách, hồ sơ bạn đọc, mượn-trả, gia hạn, thông báo và tiền phạt.

## Dữ liệu mẫu

`Data/DbSeeder.cs` và các tệp seeder liên quan khởi tạo dữ liệu nền. Khi `Seed:SampleData` bật, ứng dụng cũng thêm dữ liệu sách, bạn đọc và giao dịch mẫu để hỗ trợ chạy thử các chức năng. Các phiếu mượn mẫu chỉ được thêm khi cơ sở dữ liệu chưa có phiếu mượn.

## Đóng góp

Dự án hiện không tiếp nhận đóng góp bên ngoài.