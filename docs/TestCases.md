# Test Cases – TCP Server

## 1. Thông tin lần kiểm thử

- **Ngày thực hiện:** 24/11/2026
- **Lệnh chạy:** `dotnet run --project .\tests\Tests.csproj`
- **Server kiểm thử:** `127.0.0.1:9000`
- **Điều kiện:** Khởi động TCP Server trước khi chạy các bài kiểm thử mạng.

## 2. Kết quả tổng quan

| Kết quả | Số lượng |
|---|---:|
| PASS | 7 |
| FAIL | 0 |
| SKIP | 1 |

**Kết luận:** Tất cả 7 bài kiểm thử được thực thi đều PASS. Phần kiểm thử `MetricsCollector` thực tế được SKIP vì mã của nhóm vẫn còn TODO.

## 3. Chi tiết kết quả

| STT | Bài kiểm thử | Nội dung kiểm tra | Kết quả thực tế | Trạng thái |
|---:|---|---|---|---|
| 1 | `ProtocolTests` | Kiểm tra định dạng, đọc/ghi thông điệp, đọc dữ liệu chia nhỏ và xử lý dữ liệu không hợp lệ. | Chương trình trả về `[PASS] ProtocolTests`. | PASS |
| 2 | `MetricsTests` | Kiểm tra công thức metrics bằng dữ liệu mẫu: latency trung bình, p95, throughput và error rate. | Chương trình trả về `[PASS] MetricsTests (cong thuc du lieu mau)`. | PASS |
| 3 | `ConnectionTests` | Kiểm tra nhiều Client kết nối/gửi yêu cầu đồng thời ở các mức 1, 10 và 50 Client. | 1 Client: 1/1 response đúng; 10 Client: 10/10 response đúng; 50 Client: 50/50 response đúng. | PASS |
| 4 | `EchoTests` | Kiểm tra phản hồi Echo, đối chiếu `requestId` và payload. | Chương trình trả về `[PASS] EchoTests (requestId va payload)`. | PASS |
| 5 | `InvalidMessageTests` | Kiểm tra xử lý thông điệp sai độ dài và xác nhận Server vẫn hoạt động sau dữ liệu sai. | Chương trình trả về `[PASS] InvalidMessageTests`. | PASS |
| 6 | `TimeoutTests` | Kiểm tra timeout khi Client không gửi dữ liệu hoặc chỉ gửi một phần frame. | Chương trình trả về `[PASS] TimeoutTests (khoang 10 giay)`. | PASS |
| 7 | `DisconnectTests` | Kiểm tra Client ngắt kết nối bất thường khi Server đang chờ dữ liệu và xác nhận Server tiếp tục phục vụ Client khác. | Chương trình trả về `[PASS] DisconnectTests`. | PASS |
| 8 | `MetricsCollector` thực tế | Kiểm tra tích hợp bộ thu thập metrics thực tế của nhóm. | Chương trình thông báo `[SKIP] MetricsCollector thuc te: file nhom chua code xong.` | SKIP |

## 4. Số liệu MetricsTests với dữ liệu mẫu

Bài `MetricsTests` kiểm tra công thức bằng bộ dữ liệu mẫu, không phải số đo hiệu năng thực tế của Server.

- **Latency mẫu (ms):** 10, 20, 30, 40, 50
- **Latency trung bình:** 30 ms
- **p95 theo cách tính trong test:** 50 ms
- **Số request thành công / tổng request mẫu:** 90 / 100
- **Thời gian mẫu:** 3 giây
- **Throughput mẫu:** 30 request/giây
- **Error rate mẫu:** 10%

Kiểm thử nhiều Client được thực hiện riêng trong `ConnectionTests`: cả 1, 10 và 50 Client đều nhận được phản hồi đúng. Không xem các giá trị metrics mẫu ở trên là kết quả benchmark thực tế.

## 5. Lỗi phát sinh và cách xử lý

- **Lỗi:** Không ghi nhận lỗi trong lần chạy được cung cấp; kết quả tổng kết là `7 PASS, 0 FAIL, 1 SKIP`.
- **Cách xử lý:** Không có lỗi cần xử lý theo output lần chạy này.
- **Việc còn lại:** Hoàn thiện phần `MetricsCollector` thực tế (đang có TODO), sau đó chạy lại kiểm thử tích hợp metrics để thay trạng thái SKIP bằng kết quả thực tế.

## 6. Output tổng kết

```text
[PASS] ProtocolTests
[PASS] MetricsTests (cong thuc du lieu mau)
[SKIP] MetricsCollector thuc te: file nhom chua code xong.
  1 Client: 1/1 response dung
  10 Client: 10/10 response dung
  50 Client: 50/50 response dung
[PASS] ConnectionTests (1, 10, 50 clients)
[PASS] EchoTests (requestId va payload)
[PASS] InvalidMessageTests
[PASS] TimeoutTests (khoang 10 giay)
[PASS] DisconnectTests

Tong ket: 7 PASS, 0 FAIL, 1 SKIP
```
