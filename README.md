# TCP Server Research

Nghiên cứu ảnh hưởng của số lượng Client kết nối đồng thời đến hiệu năng TCP Server.

**C# · .NET 10 · TCP Socket · Async/Await**

## Tổng quan

Dự án gồm một TCP Server và một Load Generator chạy ở hai tiến trình riêng. Load Generator gửi request, kiểm tra phản hồi và ghi nhận năm chỉ số: **latency trung bình, P95 latency, throughput, tỷ lệ lỗi và CPU của Server**.

## Cấu hình thực nghiệm

| Kịch bản | Số lượt | Warm-up mỗi lượt | Thời gian đo mỗi lượt |
|---|---:|---:|---:|
| 1 Client | 3 | 5 giây | 20 giây |
| 10 Client | 3 | 5 giây | 20 giây |
| 50 Client | 3 | 5 giây | 20 giây |

Cả ba kịch bản sử dụng payload **1.024 byte (1 KB)**. Server và Load Generator chạy trên cùng một máy qua TCP loopback tại `127.0.0.1:9000`.

## Cấu trúc dự án

| Thư mục | Nội dung |
|---|---|
| [`src/shared/`](src/shared/) | Thư viện và giao thức dùng chung |
| [`src/server/`](src/server/) | Mã nguồn TCP Server |
| [`src/load-generator/`](src/load-generator/) | Chương trình tạo tải và đo hiệu năng |
| [`tests/`](tests/) | Bộ kiểm thử hệ thống |
| [`experiments/`](experiments/) | Cấu hình, dữ liệu đo, phân tích và biểu đồ |
| [`docs/`](docs/) | Tài liệu và báo cáo nghiên cứu |

## Hướng dẫn chạy dự án

### 1. Chuẩn bị môi trường

Cài **.NET 10 SDK**, sau đó mở PowerShell tại thư mục gốc của repo và kiểm tra phiên bản.

```powershell
dotnet --version
```

Đảm bảo cổng `9000` đang trống trước khi khởi động Server.

### 2. Chạy thử kịch bản 1 Client

1. **Khởi động TCP Server** trong cửa sổ PowerShell thứ nhất.

   ```powershell
   dotnet run --project src/server/Server.csproj -c Release -- 127.0.0.1 9000
   ```

   Khi thấy dòng `Server PID=... listening on 127.0.0.1:9000`, ghi lại số PID và giữ cửa sổ này mở.

2. **Chạy Load Generator** trong cửa sổ PowerShell thứ hai, cũng tại thư mục gốc của repo. Nhập đúng PID vừa ghi để chương trình đo CPU của Server.

   ```powershell
   $ServerPid = Read-Host "Nhap PID hien tren cua so TCP Server"
   dotnet run --project src/load-generator/LoadGenerator.csproj -c Release -- experiments/configs/clients-1.json $ServerPid
   ```

3. **Kiểm tra kết quả.** Với cấu hình mặc định, chương trình ghi `summary.csv` và 3 file `*-requests.csv` vào `experiments/raw-data/`. Nếu thư mục đã có dữ liệu cũ, đặt `OutputDirectory` trong file JSON thành thư mục mới trước khi chạy lại.

4. **Dừng Server** bằng `Ctrl+C` sau khi Load Generator chạy xong.

### 3. Chạy đủ 9 lượt đo trên Windows

Dừng Server đã mở thủ công để giải phóng cổng `9000`. Sau đó chạy lệnh sau tại thư mục gốc của repo.

```powershell
.\experiments\run-experiments.ps1
```

Script tự build bản Release, khởi động Server, đo 3 lượt ở mỗi mức 1, 10 và 50 Client, rồi dừng Server. Dữ liệu và log được lưu tại `experiments/raw-data/session-<thời-gian>/`.

Khi hoàn tất, kiểm tra thông báo `SUCCESS: 9 experiment runs recorded` và xác nhận `summary.csv` có 9 dòng kết quả.

### 4. Chạy bộ kiểm thử

Khởi động Server như ở bước 2 rồi chạy lệnh dưới đây trong cửa sổ PowerShell thứ hai. Chạy kiểm thử ngoài thời gian đo hiệu năng.

```powershell
dotnet run --project tests/Tests.csproj -c Release -- 127.0.0.1 9000
```

## Nhóm thực hiện

Dự án do nhóm 6 thành viên thực hiện.
