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

## Bổ sung bản 0.3.3 — 31/10/2026

- 63 kiểm tra đạt, gồm mount WinFsp thật bằng nguồn local dùng một lần: tạo ổ mạng, lưu DPAPI, nhận lại bằng service mới, từ chối timestamp sai và mật khẩu điều khiển sai, chặn ngắt khi còn cache upload, xác minh file đến backend local rồi ngắt sạch cả ổ lẫn session record.
- Bấm X chuyển sang thoát an toàn; không còn mặc định ẩn cửa sổ. Callback exit không làm mất quyền điều khiển phiên khác. Worker mới nhận thông tin RC qua environment.
- Chặn cấu hình ổ ghi Google nếu scope rõ ràng chỉ đọc. Điều này không thay thế quyền thực tế do nhà cung cấp kiểm soát.
- Đã xác minh phiên R: cũ mồ côi: worker còn sống, OAuth chỉ đọc, một file chờ khoảng 16 MB liên tục bị lỗi thiếu scope. Đã tạo bản sao cục bộ kiểm tra SHA256 cho file đó và nhập thông tin controller bằng DPAPI vào đúng thư mục dữ liệu ban đầu, không dừng worker. Không đưa dữ liệu/credential phục hồi vào repo hay bộ cài.
- Chưa cấp quyền ghi Google mới, chưa upload/xóa dữ liệu cloud thật trong kiểm thử này. Phiên cá nhân còn chờ cấp quyền ghi; không coi giữ cache hay nhận lại controller là bằng chứng upload thành công.

- Đã cài bản 0.3.3 lên Program Files bằng Setup, exit code 0, không yêu cầu restart. Giao diện bản cài nhận lại ổ R: và hiển thị đúng 1 ổ đang kết nối cùng thông báo khôi phục quản lý phiên.

## Bản triển khai cuối 0.3.4

- Hoãn QuitAsync qua Dispatcher sau khi sự kiện Closing kết thúc để WPF không từ chối Show/Restore trong lúc đóng. Bản 0.3.3 cục bộ được thay bằng 0.3.4; không phát hành 0.3.3 làm bản cuối.
- Setup 0.3.4 cài thành công (exit 0). Giao diện đã nhận lại R: thật, hiển thị v0.3.4 và 1 ổ đang kết nối.
- Đã thử đóng cửa sổ bằng Alt+F4: hộp thoại báo còn file chờ upload, app vẫn mở và vẫn giữ đúng kết nối. Đã đóng hộp thoại, không dừng worker hoặc sửa cache.
- 30 kiểm tra đóng gói bản 0.3.4 đạt; 63 kiểm tra backend và mount local áp dụng cho cùng mã DriveService/MountSession. Không claim upload Google thật đã thành công.

- Đã kiểm tra bản cài với cấu hình trống riêng: Alt+F4 đóng cửa sổ và tiến trình thoát. Phiên R: thật vẫn chạy riêng trong cửa sổ phục hồi.
