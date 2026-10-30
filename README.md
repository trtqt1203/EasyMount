# EasyMount 0.3

Ứng dụng miễn phí cho Windows x64: gắn từng tài khoản cloud hoặc gộp nhiều tài khoản thành một ổ trong File Explorer. Mã ứng dụng được phát hành theo MIT.

## Cài đặt

Mở `EasyMount-0.3.2-Setup.exe`, chọn Install và chấp thuận quyền quản trị để cài ứng dụng cùng WinFsp khi cần. Bộ cài chứa sẵn thành phần kết nối và WinFsp; không cần tải thêm khi cài. Mở EasyMount từ Start Menu. Nếu Windows yêu cầu khởi động lại để hoàn tất driver, thực hiện trước khi kết nối ổ.

Chỉ phát hành bản Setup. Thư mục payload do build tạo ra trong `work/` chỉ phục vụ đóng gói và kiểm thử, không phải bản portable để phân phối.

Ứng dụng cài tại `C:\Program Files\EasyMount`. Khi gỡ, WinFsp được giữ lại vì là thành phần dùng chung; cấu hình và cache cũng được giữ. Bộ Setup hiện chưa được ký bằng chứng thư phát hành.

## Thêm tài khoản cloud

1. Chọn **Tài khoản cloud**, hoặc bấm **Thêm ổ đĩa** khi chưa có tài khoản.
2. Chọn dịch vụ trong danh sách. Có thể để trống tên để lấy email sau đăng nhập, hoặc tự nhập tên. Ở màn hình hoàn tất, bạn có thể sửa tên hiển thị; mã kết nối bên trong được giữ riêng. Nếu nhà cung cấp không trả về email, hãy nhập tên bạn muốn.
3. Bấm **Đăng nhập trên trình duyệt** để xác thực tại trang của nhà cung cấp. Khi cần chọn loại tài khoản/ổ, trả lời ngay trong cửa sổ EasyMount. WebDAV, SFTP, S3 và các dịch vụ khác sử dụng biểu mẫu tương ứng trong ứng dụng; không mở terminal.
4. Chờ EasyMount kiểm tra quyền truy cập, bấm **Hoàn tất**. Nếu bắt đầu từ Thêm ổ đĩa, form tạo ổ sẽ mở tiếp. Có thể thêm nhiều tài khoản cùng dịch vụ; ứng dụng không giới hạn số lượng.
5. Có thể **Hủy** khi đang xác thực. Tài khoản mới chưa hoàn tất sẽ được dọn nếu dịch vụ cấu hình phản hồi; các tài khoản cũ được giữ. Nếu ứng dụng bị buộc dừng, có thể còn cấu hình dở cần kiểm tra lại.

Google Drive cần OAuth client riêng để sử dụng ổn định: client dùng chung của rclone đang được ngừng trong năm 2026. Khi bản build chưa được cấu hình OAuth của EasyMount, giao diện sẽ hướng dẫn nhập Client ID và Client Secret. Nguồn chính thức: https://rclone.org/drive/#making-your-own-client-id . Không coi việc có giao diện đăng nhập là bằng chứng đã xác thực thành công bằng tài khoản thật.

Bản 0.3.2 đã có OAuth client Desktop riêng của EasyMount trong `oauth-clients.json`; không cần nhập Client ID/Secret khi thêm Google Drive. File này chỉ chứa định danh ứng dụng, không chứa token, mật khẩu hoặc cấu hình tài khoản người dùng. Đây là public client của ứng dụng Desktop; không dùng client secret đi kèm như mật khẩu bảo vệ một dịch vụ máy chủ.

OAuth Google hiện ở trạng thái **External / Testing**, chỉ dành cho tài khoản trong danh sách tester; tối đa 100 tester và refresh token cho quyền Drive hết hạn sau 7 ngày. Bản này chưa phải đăng nhập công khai đã được Google xét duyệt. Muốn phát hành rộng rãi cần hoàn thiện branding, trang chủ/chính sách riêng tư trên miền đã xác minh và quy trình xét duyệt phạm vi Drive. Chi tiết: `docs/OAUTH-SETUP.md`; [giới hạn Testing của Google](https://support.google.com/cloud/answer/15549945?hl=en).

Tên remote khác nhau không bảo đảm đó là hai tài khoản thực khác nhau. Tránh cấu hình cùng một tài khoản/thư mục dưới nhiều tên rồi đưa vào cùng một ổ gộp vì dung lượng có thể bị đếm lặp.

## Tạo một ổ gộp

1. Chọn **Thêm ổ đĩa**, nhập tên hiển thị và ký tự ổ, ví dụ `R:`.
2. Bật **Gộp nhiều tài khoản thành một ổ đĩa**.
3. Chọn tài khoản và thư mục, nhập giới hạn nếu cần, bấm **Thêm nguồn vào ổ gộp**. Thêm ít nhất hai nguồn.
4. Dùng **Lên / Xuống** để sắp thứ tự ưu tiên. Nguồn ở trên được xét trước.
5. Chọn **Kiểm tra**, lưu, rồi kết nối. Mặc định chỉ đọc; bỏ chọn chỉ đọc khi muốn ghi lên cloud.

File mới được ghi vào nguồn đầu tiên còn đủ chỗ chứa toàn bộ file. Ví dụ nguồn 1 còn 2 GiB, nguồn 2 còn 20 GiB: file 3 GiB sẽ được ghi nguyên vào nguồn 2. File nhỏ hơn vẫn có thể sử dụng dung lượng còn lại ở nguồn 1. Không chia file giữa nhiều tài khoản, không di chuyển hay cân bằng lại file cũ khi đổi thứ tự.

Giới hạn **0** dùng dung lượng trống do dịch vụ báo. Nếu dịch vụ không có API báo dung lượng, đặt giới hạn GiB cho thư mục đã chọn. Giới hạn thủ công áp dụng cho việc đặt **file mới**, tính tổng file trong thư mục đó; kiểm tra này cần quét thư mục và có thể chậm trên kho lớn. Nếu có cả quota API và giới hạn thủ công, dùng mức còn trống thấp hơn. Sửa file cũ giữ nguyên nơi lưu; quota vật lý của nhà cung cấp vẫn áp dụng.

Khi không nguồn nào đủ chỗ, thao tác upload báo lỗi và cache VFS được giữ lại. Khi quota API bị trễ hoặc cloud phát sinh lỗi trong lúc upload, giữ ứng dụng chạy và xử lý thông báo; không xem việc Windows đã ghi xong vào cache là upload đã hoàn tất. Lỗi mạng, quyền truy cập và hạn mức API không được coi là hết dung lượng.

Các thư mục cùng tên được hiển thị chung. Nếu nhiều nguồn đã có file cùng đường dẫn, bản ở nguồn ưu tiên cao hơn được đọc; thao tác sửa/xóa theo chính sách union `epall` có thể tác động các bản trùng. Nên dùng thư mục riêng, rỗng khi tạo ổ gộp mới.

## Đổi tên và vận hành

- Ngắt kết nối, chọn **Sửa**, đổi tên rồi lưu và kết nối lại. Tên mới được dùng làm nhãn ổ trong Explorer. Đổi tên không đổi tài khoản, thứ tự hoặc khóa phục hồi cache.
- Có thể đổi thứ tự và nguồn khi ổ đã ngắt và không còn cache cần giữ. Bản 0.2 chặn đổi/xóa nguồn nếu thư mục cache vẫn có file để tránh chuyển nhầm dữ liệu chờ upload. Kết nối bằng cấu hình cũ để hoàn tất phục hồi; chỉ dọn cache sau khi xác minh dữ liệu đã có trên cloud.
- Trước khi ngắt ổ ghi, đóng các file đang mở và chờ upload hoàn tất. Không xóa cache có dữ liệu chưa tải lên.
- Đóng cửa sổ sẽ thu về khay hệ thống. Dùng **Thoát ứng dụng** để ngắt ổ và thoát.
- Tự kết nối chỉ áp dụng khi mở EasyMount, không tự đăng ký chạy cùng Windows.
- Đây là truy cập cloud theo nhu cầu, không phải một bản sao ngoại tuyến đầy đủ.

Cấu hình nằm ở `%LOCALAPPDATA%\EasyMount`: `drives.json`, `rclone.conf`, `pools`, `logs`, `cache`. `rclone.conf` có thể chứa token. Không đưa cấu hình cá nhân vào repo.

## Build và kiểm tra

Cần .NET 8 SDK, Go 1.26 trở lên và WiX 5.0.2. Engine là rclone v1.75.2 có bản vá nguồn nhỏ trong `engine/`; bản rclone upstream không có bộ chọn ghi theo thứ tự và kích thước file này.

```powershell
.\build-setup.ps1 -DotnetPath 'C:\path\dotnet.exe' -GoPath 'C:\path\go.exe'
# Hoặc build payload riêng rồi đóng gói:
.\build.ps1 -DotnetPath 'C:\path\dotnet.exe' -GoPath 'C:\path\go.exe'
.\build-setup.ps1 -SkipAppBuild
```

Script tải mã nguồn rclone với hash đã ghim, áp bản vá và build engine có Windows mount (`cmount`). WinFsp MSI được kiểm tra hash và chữ ký chính thức trước khi đóng gói. Không script nào tự cài app hoặc driver lên máy build.

```powershell
dotnet run --project .\tests\EasyMount.Checks.csproj -- C:\path\work\easymount-build\app\tools\rclone.exe
python .\tests\check-pool.py C:\path\work\easymount-build\app\tools\rclone.exe
```

Xem `docs/VALIDATION.md` về kiểm chứng và phần chưa được thử. Tài liệu upstream: [rclone union](https://rclone.org/union/), [rclone mount](https://rclone.org/commands/rclone_mount/), [Google Drive](https://rclone.org/drive/), [OneDrive](https://rclone.org/onedrive/).

## Repo và website

- Mã nguồn: https://github.com/trtqt1203/EasyMount
- Trang chủ: https://trtqt1203.github.io/EasyMount/
- Chính sách riêng tư: https://trtqt1203.github.io/EasyMount/privacy.html

Source công khai dùng `oauth-clients.example.json` trống nếu không có file cấu hình OAuth cục bộ. Sao chép thành `oauth-clients.json` và điền định danh client riêng trước khi build bản phát hành có đăng nhập Google được cấu hình sẵn. Không commit file đó, token, rclone.conf, drives.json hoặc account-labels.json. Bộ cài phát hành chính thức được build với định danh ứng dụng riêng của EasyMount; tài khoản người dùng vẫn phải được Google cho phép và tự cấp quyền.
