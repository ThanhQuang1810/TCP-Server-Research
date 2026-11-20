# TCP Server Research

## Giới thiệu

Dự án nghiên cứu ảnh hưởng của số lượng Client kết nối đồng thời đến hiệu năng TCP Server.

Hệ thống gồm TCP Server và Load Generator, được xây dựng bằng C# trên nền tảng .NET 10, sử dụng TCP Socket và mô hình xử lý bất đồng bộ (Async/Await).

## Mục tiêu

- Xây dựng TCP Server phục vụ nhiều Client đồng thời.
- Thực nghiệm với 1, 10 và 50 Client.
- Đo lường Latency, P95 Latency, Throughput, Error Rate và CPU Usage.
- Thu thập, phân tích dữ liệu và đánh giá hiệu năng Server.

## Công nghệ sử dụng

- C# – .NET 10
- TCP Socket
- Async/Await
- Python
- Git và GitHub

## Cấu trúc dự án

- `src/shared`: Thư viện và giao thức dùng chung.
- `src/server`: TCP Server.
- `src/load-generator`: Chương trình tạo tải và đo hiệu năng.
- `tests`: Kiểm thử hệ thống.
- `experiments`: Cấu hình, dữ liệu thực nghiệm, phân tích và biểu đồ.
- `docs`: Tài liệu và báo cáo nghiên cứu.

## Trạng thái

Dự án đang trong quá trình phát triển bởi nhóm gồm 6 thành viên.
