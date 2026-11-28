const logout = document.getElementById("smartmart-logout");
logout?.addEventListener("click", async () => {
    logout.disabled = true;
    const message = document.getElementById("smartmart-logout-message");
    try {
        const name = "XSRF-TOKEN=";
        const cookie = document.cookie.split(";").map(value => value.trim()).find(value => value.startsWith(name));
        const response = await fetch("/api/v1/auth/logout", {
            method: "POST",
            headers: { "X-XSRF-TOKEN": cookie ? decodeURIComponent(cookie.slice(name.length)) : "" }
        });
        if (!response.ok && response.status !== 401) throw new Error("Chưa đăng xuất được. Hãy tải lại trang.");
        location.assign("/login.html");
    } catch (error) {
        message.textContent = error.message;
        logout.disabled = false;
    }
});
