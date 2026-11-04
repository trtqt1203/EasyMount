# Kế hoạch phát hành EasyMount

Ngày lập: 04/11/2026. Phạm vi: app Windows x64 miễn phí, bộ cài Setup, Google Drive / OneDrive / Dropbox, website tải app và thông tin phát hành. Đây là kế hoạch; chưa tạo tag, tải bộ cài lên GitHub hoặc mở OAuth cho cộng đồng.

## Hiện trạng đã kiểm tra

- Bản desktop hiện tại: 0.6.15, WPF, target `net8.0-windows`; có bộ cài EXE/MSI, engine rclone tùy chỉnh và WinFsp.
- Repo `https://github.com/trtqt1203/EasyMount` đang công khai.
- Bộ cài 0.6.15 có trạng thái chữ ký `NotSigned`.
- API công khai `/api/releases` trả `status: unpublished`, `version: null`, `downloadUrl: null` tại thời điểm rà soát.
- App kiểm tra phiên bản và mở liên kết Setup chính thức; API website đọc manifest được kiểm tra schema. Có `web/scripts/sync-release.mjs` để rà soát/tái sử dụng cho khâu cập nhật metadata.
- README vẫn ghi 0.6.3 và còn mô tả một số dịch vụ/hành vi cũ. Khung minh họa website vẫn ghi 0.6.11, khác bản desktop.
- Có kiểm tra tự động và bằng chứng vận hành thật trên máy phát triển, nhưng cần một vòng kiểm thử phát hành trên Windows sạch và tài khoản thật cho cả ba nhà cung cấp.
- Tài liệu hiện có ghi Google OAuth External/Testing. Cần xác minh lại trực tiếp Cloud Console trước khi chốt trạng thái phát hành; không xem các bản sửa giao diện là bằng chứng OAuth đã được duyệt.

## Hướng phát hành

Phát hành Beta 0.7.x trước, sau đó chốt bản Stable khi các điều kiện bên dưới đều đạt. Mỗi bộ cài beta phải tăng số phiên bản MSI; tag GitHub có thể có hậu tố beta, còn số phiên bản mà API/app hiện tại xử lý phải là ba phần số. Beta dùng GitHub Pre-release và liên kết thử nghiệm rõ ràng; kênh cập nhật Stable chỉ trỏ tới bản đã được chấp nhận.

Giữ app miễn phí, nguồn công khai và ba dịch vụ đã chọn. Backend website phục vụ metadata phát hành; truyền file và thông tin xác thực cloud tiếp tục ở máy người dùng.

## Giai đoạn 1 — Chốt nền tảng và phạm vi

Ước lượng: 1–2 ngày làm việc.

- Chuyển app và dự án kiểm thử lên .NET 10 LTS cùng bản vá đang được hỗ trợ. .NET 8 hết hỗ trợ ngày 10/11/2026, nên không dùng nó làm nền tảng cho đợt phát hành mới.
- Kiểm tra lại build WPF, đóng gói runtime, engine, WinFsp và cập nhật từ 0.6.15 sau khi nâng runtime.
- Chốt danh sách tính năng; tập trung sửa lỗi ảnh hưởng dữ liệu, đăng nhập, cài đặt và khả năng đọc giao diện.
- Chốt Windows 11 x64 là môi trường kiểm thử chính. Chỉ công bố thêm phiên bản Windows khi đã kiểm thử và đối chiếu chính sách hỗ trợ runtime/driver.

Đầu ra: bản build ứng viên có nguồn và phiên bản xác định; danh sách tính năng/OS hỗ trợ và lỗi còn biết.

## Giai đoạn 2 — Đăng nhập cloud và hồ sơ phát hành

Ước lượng triển khai nội bộ: 2–3 ngày; thời gian xét duyệt bên ngoài tách riêng.

- Đối chiếu Google Cloud: client Desktop, Audience, scopes, branding, trạng thái verification, quota và tài khoản hỗ trợ.
- Quyền đọc/ghi toàn bộ Drive phục vụ gắn ổ thuộc nhóm restricted. Chuẩn bị mô tả nhu cầu quyền, sơ đồ dữ liệu, video đăng nhập/sử dụng, trang chủ và chính sách riêng tư đáp ứng yêu cầu của Google.
- Chốt tên miền và phương án xác minh quyền sở hữu website được Google chấp nhận; ưu tiên một tên miền chủ dự án kiểm soát lâu dài.
- Giữ đường truyền cloud trực tiếp từ desktop đến nhà cung cấp. Xác định yêu cầu security assessment theo kiến trúc thực tế; không đưa file/token lên backend để thuận tiện xét duyệt.
- Hoàn thiện hồ sơ Production/verification theo Google. Chuyển trạng thái Production đơn thuần không phải bằng chứng đã hết giới hạn ứng dụng chưa xác minh.
- Kiểm tra OneDrive và Dropbox với tài khoản thật, nhiều tài khoản độc lập, email hiển thị đúng, hủy đăng nhập, token hết hạn/thu hồi quyền và quyền đọc/ghi.

Đầu ra: bảng nhà cung cấp / luồng đã thử / quyền yêu cầu / trạng thái xét duyệt / giới hạn còn áp dụng. Beta có giới hạn tester phải ghi rõ phạm vi; phát hành rộng Google Drive chỉ khi điều kiện truy cập công khai đã được xác minh.

## Giai đoạn 3 — Vòng kiểm thử an toàn dữ liệu

Ước lượng: 2–3 ngày, dùng thư mục và dữ liệu thử riêng.

- Với từng dịch vụ: đăng nhập hai tài khoản, upload/download, đổi tên, sửa nội dung, xóa file thử, file lớn và tên tiếng Việt. Kiểm tra kích thước và checksum khi API hỗ trợ.
- Ổ gộp năm nguồn: đọc danh sách thống nhất; ghi theo ưu tiên; bỏ qua nguồn không đủ chỗ cho cả file; hết quota ở mọi nguồn; quota chậm; lỗi mạng/quyền không bị hiểu nhầm thành hết chỗ.
- Chốt và kiểm thử hành vi khi trùng đường dẫn: đọc, sửa và xóa tác động nguồn nào. Tài liệu và UI phải khớp với hành vi thực tế.
- Mất mạng, token hết hạn, app/worker bị gián đoạn, Windows khởi động lại: xác minh cache/hàng đợi còn phục hồi được.
- Thoát, ngắt, nâng cấp và gỡ app khi còn upload phải chặn hoặc chờ theo cơ chế an toàn; không bỏ qua bảo vệ để hoàn thành kiểm thử.
- Workspace: Enter, tạo/xóa nhóm, gán ổ, lưu qua lần mở sau; xóa nhóm không xóa ổ/file.
- Giao diện EN/VI, sáng/tối, cửa sổ thường/toàn màn hình và DPI 100/125/150/200%; kiểm tra cắt chữ và thao tác bàn phím.

Điều kiện đạt: không còn lỗi nghiêm trọng gây mất/ghi nhầm/xóa nhầm dữ liệu; đường phục hồi đã được thử; mỗi dịch vụ quảng bá đều có bằng chứng end-to-end thật. Nếu một dịch vụ chưa đạt, xử lý hoặc công bố rõ là thử nghiệm thay vì mô tả đã hoàn thiện.

## Giai đoạn 4 — Bộ cài và khả năng bảo trì

Ước lượng: 1–2 ngày; việc cấp phương tiện ký phụ thuộc nhà cung cấp.

- Kiểm thử trên máy Windows sạch: chưa có WinFsp, đã có WinFsp, tài khoản thường/UAC, yêu cầu khởi động lại, cập nhật từ 0.6.15, cài lại và gỡ khi có hàng đợi.
- Chọn phương án ký mã phù hợp với chủ dự án/ngân sách. Ký các thành phần EasyMount tự build, MSI và Setup theo đúng thứ tự đóng gói; giữ chữ ký WinFsp gốc.
- Kiểm tra chữ ký, timestamp và hash sau khi đóng gói. Có chữ ký không đảm bảo hết cảnh báo SmartScreen ngay ở lần phát hành đầu.
- Rà soát gói phát hành để không có cấu hình tài khoản, token, cache, log cá nhân hoặc thông tin thử nghiệm.
- Giữ giấy phép MIT của app/rclone và đầy đủ thông báo, giấy phép, mã nguồn tương ứng của thành phần bên thứ ba đang phân phối.
- Chuẩn hóa quy trình build trên Windows CI, ghim đầu vào, lưu báo cáo kiểm tra và tạo artifact có thể đối chiếu với tag nguồn.

Đầu ra: Setup, SHA-256 của file cuối cùng, ghi chú phiên bản, nguồn của tag, thông báo thành phần bên thứ ba và báo cáo kiểm thử. Bản Beta chưa ký chỉ dùng khi giới hạn này được công bố rõ; mục tiêu Stable là bộ cài có chữ ký hợp lệ.

## Giai đoạn 5 — Đồng bộ website và cập nhật

Ước lượng: khoảng 1 ngày sau khi artifact đã chốt.

- Cập nhật README, hướng dẫn, trang riêng tư và minh họa: phiên bản, font/giao diện, ngôn ngữ/chủ đề, copyright 2026, đúng ba dịch vụ hỗ trợ và giới hạn thực tế.
- Tạo release GitHub từ tag đã kiểm thử, đính kèm đúng Setup và hash đã xác minh. Tải artifact từ liên kết GitHub để kiểm tra độc lập.
- Cập nhật manifest từ release đó; xác minh phiên bản, liên kết, dung lượng, ngày phát hành và trường `signed` phản ánh chữ ký thực tế.
- Chỉ sau đó triển khai API/website để nút tải xuống và chức năng kiểm tra cập nhật trỏ tới bản thực sự tồn tại.
- Thử toàn luồng: truy cập website → tải Setup → cài trên Windows sạch → đăng nhập → gắn ổ → upload/download → kiểm tra bản cập nhật.

Đầu ra: một đường tải chính thức thống nhất giữa website, API và app. Các metadata phiên bản lấy từ cùng nguồn phát hành, không điền phiên bản mới chỉ để đồng bộ giao diện.

## Giai đoạn 6 — Beta, Stable và xử lý sự cố

- Mời một nhóm nhỏ dùng Beta trong 5–7 ngày với hướng dẫn rõ và mẫu báo lỗi đã loại thông tin nhạy cảm. Không bật gửi log/token tự động.
- Theo dõi đăng nhập, tốc độ và lỗi upload, phục hồi cache, cài/gỡ và khả năng đọc ở nhiều màn hình.
- Sửa lỗi rồi tăng phiên bản bộ cài; không ghi đè artifact đã công bố bằng nội dung khác.
- Chốt Stable khi các điều kiện phát hành đều đạt. Không cam kết ngày Stable trước khi biết kết quả OAuth và vòng Beta.
- Khi có lỗi nghiêm trọng: gỡ bản lỗi khỏi kênh cập nhật, giữ hướng dẫn phục hồi và artifact cũ. Chỉ hướng dẫn hạ phiên bản sau khi đã xác minh tương thích cấu hình/cache; ưu tiên bản sửa tăng phiên bản để phù hợp MSI.

## Lịch dự kiến và việc cần chủ dự án quyết định

Khoảng 7–12 ngày làm việc cho chuẩn bị nội bộ, một số việc làm song song; thêm 5–7 ngày Beta. Thời gian Google xét duyệt, xác minh tên miền và cấp phương tiện ký không nằm trong ước lượng này.

Các quyết định cần chốt khi bắt đầu thực hiện: kênh beta hay công khai rộng ngay từ đầu; tên miền kiểm soát được; phương án/ngân sách ký mã; tài khoản thử của ba nhà cung cấp. Mọi hành động mua dịch vụ hoặc phát hành công khai sẽ được xử lý riêng, sau khi có artifact cụ thể để xem xét.

## Nguồn chính thức

- .NET support: https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- Google OAuth Testing/token lifetime: https://developers.google.com/identity/protocols/oauth2
- Drive scopes: https://developers.google.com/workspace/drive/api/guides/api-specific-auth
- Restricted scope verification/security assessment: https://developers.google.com/identity/protocols/oauth2/production-readiness/restricted-scope-verification
- SmartScreen/signing: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation
