
document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("loginForm");
    const message = document.getElementById("message");

    if (!form || !message) return;

    const password = document.getElementById("password");
    const passwordToggle = document.getElementById("passwordToggle");
    if (passwordToggle) {
        passwordToggle.hidden = false;
        passwordToggle.addEventListener("click", () => {
            const show = password.type === "password";
            password.type = show ? "text" : "password";
            passwordToggle.textContent = show ? "Ẩn" : "Hiện";
        });
    }

    form.addEventListener("submit", async (event) => {
        event.preventDefault();

        const tenDangNhap =
            document.getElementById("username").value.trim();

        const matKhau = password.value;

        const button = form.querySelector(
            'button[type="submit"]'
        );

        message.textContent = "";
        button.disabled = true;
        button.classList.add("is-loading");
        button.setAttribute("aria-busy", "true");

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

            const isJson = response.headers.get("content-type")?.includes("json");
            const data = isJson ? await response.json() : null;

            if (!response.ok) {
                message.textContent =
                    data?.message ||
                    data?.thongBao ||
                    "Đăng nhập thất bại. Kiểm tra tài khoản và mật khẩu.";
                return;
            }

            if (!data) {
                message.textContent = "Máy chủ trả phản hồi không hợp lệ. Vui lòng thử lại.";
                return;
            }

            if (data.batBuocDoiMatKhau) {
                message.textContent =
                    "Tài khoản cần đổi mật khẩu trước khi sử dụng.";
                return;
            }

            window.location.assign("/Home/Workspace");

        } catch (error) {
            console.error("Lỗi đăng nhập:", error);
            message.textContent =
                "Không kết nối được máy chủ. Hãy kiểm tra ứng dụng.";
        } finally {
            button.disabled = false;
            button.classList.remove("is-loading");
            button.removeAttribute("aria-busy");
        }
    });
});
1
