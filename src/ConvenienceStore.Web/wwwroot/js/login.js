
document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("loginForm");
    const message = document.getElementById("message");

    if (!form || !message) return;

    form.addEventListener("submit", async (event) => {
        event.preventDefault();

        const tenDangNhap =
            document.getElementById("username").value.trim();

        const matKhau =
            document.getElementById("password").value;

        const button = form.querySelector(
            'button[type="submit"]'
        );

        message.textContent = "";
        button.disabled = true;
        button.textContent = "Đang đăng nhập...";

        try {
            const response = await fetch("/api/v1/auth/login", {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    tenDangNhap: tenDangNhap,
                    matKhau: matKhau
                })
            });

            const data = await response.json();

            if (!response.ok) {
                message.textContent =
                    data.message ||
                    data.thongBao ||
                    "Đăng nhập thất bại. Kiểm tra tài khoản và mật khẩu.";
                return;
            }

            if (data.batBuocDoiMatKhau) {
                message.textContent =
                    "Tài khoản cần đổi mật khẩu trước khi sử dụng.";
                return;
            }

            // Chỉ cho phép chuyển hướng nội bộ
            const destination = data.manHinhChinh;

            if (typeof destination === "string" &&
                destination.startsWith("/") &&
                !destination.startsWith("//") &&
                !destination.startsWith("/\\")) {
                window.location.assign(destination);
            } else {
                window.location.assign("/");
            }

        } catch (error) {
            console.error("Lỗi đăng nhập:", error);
            message.textContent =
                "Không kết nối được máy chủ. Hãy kiểm tra ứng dụng.";
        } finally {
            button.disabled = false;
            button.textContent = "Đăng nhập";
        }
    });
});
1