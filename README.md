### TCP Server Research
Giới thiệu
## Dự án nghiên cứu ảnh hưởng của số lượng Client kết nối đồng thời đến hiệu năng TCP Server.

Hệ thống gồm TCP Server và Load Generator, được xây dựng bằng C# trên nền tảng .NET 10, sử dụng TCP Socket và mô hình xử lý bất đồng bộ (Async/Await).
Mục tiêu
- Xây dựng TCP Server phục vụ nhiều Client đồng thời.
- Thực nghiệm với 1, 10 và 50 Client.
- Đo lường Latency, P95 Latency, Throughput, Error Rate và CPU Usage.
- Thu thập, phân tích dữ liệu và đánh giá hiệu năng Server.
Công nghệ sử dụng
- C# – .NET 10
- TCP Socket
- Async/Await
- Python
- Git và GitHub
Cấu trúc dự án
- src/shared: Thư viện và giao thức dùng chung.
- src/server: TCP Server.
- src/load-generator: Chương trình tạo tải và đo hiệu năng.
- tests: Kiểm thử hệ thống.
- experiments: Cấu hình, dữ liệu thực nghiệm, phân tích và biểu đồ.
- docs: Tài liệu và báo cáo nghiên cứu.
Hướng dẫn chạy dự án
Cần cài .NET 10 SDK. Trên Windows, mở PowerShell tại thư mục gốc của repo và kiểm tra phiên bản bằng dotnet --version. Đảm bảo cổng 9000 đang trống trước khi khởi động Server.
Chạy thử kịch bản 1 Client
1. Mở PowerShell thứ nhất và chạy TCP Server.
   dotnet run --project src/server/Server.csproj -c Release -- 127.0.0.1 9000
   Đợi dòng Server PID=... listening on 127.0.0.1:9000. Ghi lại PID và giữ cửa sổ Server mở.
2. Mở PowerShell thứ hai tại thư mục gốc. Nhập PID vừa ghi để Load Generator đo CPU của Server.
   $ServerPid = Read-Host "Nhap PID hien tren cua so TCP Server"
   dotnet run --project src/load-generator/LoadGenerator.csproj -c Release -- experiments/configs/clients-1.json $ServerPid
   Kịch bản chạy 3 lượt với payload 1.024 byte, warm-up 5 giây và đo 20 giây mỗi lượt. Kết quả nằm trong thư mục được chỉ định bởi OutputDirectory trong file JSON. Với cấu hình mặc định, đó là experiments/raw-data/, gồm summary.csv và 3 file *-requests.csv. Nếu thư mục đã có dữ liệu cũ, hãy đặt OutputDirectory thành một thư mục mới trước khi chạy lại.
3. Sau khi Load Generator chạy xong, nhấn Ctrl+C trong cửa sổ Server để dừng.
Chạy đủ 9 lượt đo trên Windows
Dừng Server đã mở thủ công để giải phóng cổng 9000. Từ thư mục gốc, chạy:
.\experiments\run-experiments.ps1
Script tự build bản Release, mở Server, chạy 3 lượt cho mỗi mức 1, 10 và 50 Client rồi dừng Server. Kết quả, cấu hình từng kịch bản và log được lưu trong experiments/raw-data/session-<thời-gian>/. Khi chạy xong, kiểm tra thông báo SUCCESS: 9 experiment runs recorded và 9 dòng kết quả trong summary.csv.
Chạy bộ kiểm thử
Khởi động Server như bước 1 và chạy lệnh dưới đây ở cửa sổ PowerShell thứ hai. Chạy kiểm thử ngoài thời gian đo hiệu năng.
dotnet run --project tests/Tests.csproj -c Release -- 127.0.0.1 9000
Trạng thái
Dự án đang trong quá trình phát triển bởi nhóm gồm 6 thành viên.
