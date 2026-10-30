# Kiểm chứng EasyMount 0.3

Ngày: 30/10/2026 (Asia/Bangkok).

## Đã kiểm chứng

- Build WPF .NET 8 self-contained Windows x64 thành công; bộ phát hành chỉ có Setup.
- 48 kiểm tra ứng dụng đạt: lưu cấu hình, phục hồi lỗi cấu hình, plan ổ gộp có thứ tự và giới hạn dung lượng, 1.000 nguồn, đổi tên, chặn thay đổi routing khi còn cache, WebDAV loopback thật và thông báo thiếu WinFsp.
- 18 kiểm tra engine đạt trên dữ liệu dùng một lần: ghi ưu tiên, dùng đầy nguồn đầu, chuyển nguồn khi file không vừa, tổng hợp danh sách, báo hết chỗ, giữ vị trí khi sửa file cũ, đổi thứ tự, đọc file trùng theo thứ tự, sao chép trong pool, quota tự động, quota thủ công và cấu hình union lồng nhau.
- Engine `v1.75.2-easymount.1` build từ mã nguồn rclone v1.75.2 với overlay trong `engine/`, có tag `cmount`. Đây là bản sửa đổi có nguồn, không phải binary chính thức của rclone. Hash executable được ghi trong `BUILD.json`.
- 30 kiểm tra bộ cài bằng công cụ WiX thông thường: driver trước app, tất cả payload nhúng, hash MSI WinFsp và chữ ký gốc hợp lệ, bỏ qua WinFsp cùng/mới hơn, giữ driver khi gỡ, đường dẫn Program Files, Start Menu và hash các file ứng dụng. Không có cấu hình tài khoản trong bộ cài. Kết quả raw: `installer-results.txt`.
- Kiểm tra giao diện Windows thực tế bằng dữ liệu thử riêng: đọc tên và hai nguồn, đổi tên thành “Kho tổng EasyMount”, đưa OneDrive-demo lên trước Google-demo, lưu và đọc lại cả trạng thái giao diện lẫn JSON. Đã kiểm tra lại form có nút Kiểm tra/Lưu luôn hiển thị; ảnh tại `screenshots/pool-editor.jpg`.

Kết quả raw ứng dụng và engine: `backend-results.txt`, `pool-results.txt`. Các kiểm tra không dùng tài khoản cloud cá nhân. Không có REA Evidence trong báo cáo này; phần kiểm tra package sử dụng WiX.

## Chưa kiểm chứng và giới hạn

- Chưa cài Setup lên hệ thống, chưa gắn ổ Windows thật, đã kiểm tra phiên Google thật bằng truy vấn quota chỉ đọc; chưa thử Microsoft thật. WinFsp hiện đã có trên máy; kiểm thử này không tạo ổ Windows thật.
- Chưa thử lỗi trong lúc upload, mất điện, mạng chập chờn hoặc sử dụng dài hạn. Kiểm tra lựa chọn nguồn dùng các thư mục và backend thử, không chứng minh quota Google/OneDrive cập nhật ngay lập tức.
- Chọn nguồn áp dụng cho file mới, cả file phải vừa trên một nguồn. File cũ giữ vị trí khi sửa; không tự cân bằng lại hoặc chia file.
- Không có giới hạn số tài khoản do ứng dụng đặt ra. RAM, thời gian mở nhiều remote và hạn mức dịch vụ vẫn là giới hạn thực tế; kiểm tra 1.000 nguồn là kiểm tra cấu hình, không phải 1.000 phiên cloud thật.
- Nếu cloud không báo dung lượng trống, cần giới hạn thư mục thủ công. Cách này quét tổng dung lượng thư mục trước khi đặt file, có thể chậm trên kho lớn. Quota dịch vụ có thể trễ; lỗi upload giữ lại cache để xử lý/phục hồi, không được báo giả là đồng bộ thành công.
- Tên trùng giữa các nguồn được đọc theo thứ tự. Chính sách action `epall` có thể cập nhật/xóa nhiều bản cùng đường dẫn; xem README trước khi gộp các thư mục đã có dữ liệu.
- Bản 0.2 chặn thay đổi/xóa nguồn khi còn file cache, kể cả cache chưa được dọn sau một upload thành công. Đổi tên hiển thị vẫn được phép; không xóa cache trước khi xác minh upload hoàn tất.
- Bộ Setup EasyMount chưa có chữ ký phát hành; MSI WinFsp đi kèm giữ nguyên chữ ký chính thức.

## Bổ sung bản 0.3

- Thay console cấu hình bằng cửa sổ native: chọn dịch vụ, tên tài khoản, câu hỏi có lựa chọn, mật khẩu che, trạng thái chờ trình duyệt và hủy.
- Đã thử state machine WebDAV thật qua RC được bảo vệ bằng thông tin xác thực ngẫu nhiên, từ chối request không xác thực, chặn trùng tên, giữ tài khoản hoàn tất và dọn phiên hủy.
- Đã thử khởi động callback OAuth Google Drive trên loopback bằng client giả và tắt tự mở trình duyệt: xác nhận trạng thái chờ, hủy và dọn phiên. Không đăng nhập Google hoặc gửi mã xác thực/token thật.
- Hỗ trợ oauth-clients.json cho định danh OAuth của ứng dụng. Bản 0.3.1 đã tích hợp client Desktop EasyMount Windows của project easymount-513208; Google Drive API được bật, consent External / Testing. Không có token người dùng trong bản build; đã xác nhận phiên Google thật đọc được quota; chưa xét duyệt cho cộng đồng.
- Đã xem cửa sổ main và form thêm tài khoản native thực tế. Ảnh: screenshots/account-setup-v3.jpg. Giao diện có logo mới; có ba lựa chọn logo trong docs/brand/logo-options.png. Các thao tác form WebDAV được kiểm chứng bằng kiểm thử state machine, chưa xác nhận toàn bộ bằng chuột qua giao diện.

## Bổ sung bản 0.3.1

- OAuth Desktop EasyMount đã cấu hình, tích hợp và kiểm tra quota Google thật thành công. Chi tiết trong OAUTH-SETUP.md.
- 48 kiểm tra ứng dụng đạt; 30 kiểm tra bộ cài đạt, gồm hash oauth-clients.json trong MSI trùng payload build. Không kiểm thử ghi/xóa cloud.

## Bổ sung bản 0.3.2

- Bỏ ba nút dịch vụ trùng với danh sách chọn. Tên ban đầu tùy chọn; sau đăng nhập lấy email Google/OneDrive/Dropbox khi API trả về và cho sửa trước khi hoàn tất. Dịch vụ không cung cấp email dùng tên dịch vụ làm gợi ý để sửa.
- Tên hiển thị lưu riêng trong account-labels.json; không đổi mã remote hoặc routing của ổ đã cấu hình. Không đặt giới hạn số tài khoản.
- 52 kiểm tra ứng dụng đạt, gồm lưu/đổi tên email, kiểm tra ký tự điều khiển và hai remote độc lập có cùng tên hiển thị. Ba kiểm tra OAuth callback có thể SKIP nếu cổng 53682 đang được phiên đăng nhập khác dùng; lần chạy cuối hiện đủ 52 PASS.
- Đã kiểm tra Google about user(emailAddress) với phiên OAuth có sẵn: email hợp lệ, không in giá trị/token. Chưa kiểm tra email OneDrive/Dropbox thật.
- Lỗi 403 quan sát trên trang Google xác định tài khoản trtqt1203@gmail.com chưa được cho phép trong chế độ Testing. Đây là hạn chế cấu hình Google OAuth; không thể bỏ hạn chế bằng cách đổi tên tài khoản trong app.
- Trang tĩnh giới thiệu và privacy.html không dùng script phân tích, form đăng nhập hoặc phụ thuộc bên ngoài. Source công khai loại cấu hình OAuth cục bộ và tài khoản người dùng.
