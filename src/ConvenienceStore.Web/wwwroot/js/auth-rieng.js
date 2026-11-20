"use strict";
const mode = document.body.dataset.mode;
const form = document.querySelector("#auth-form");
const message = document.querySelector("#message");
const button = form.querySelector("button[type=submit]");
const field = id => document.querySelector("#" + id);
const token = mode === "reset" ? new URLSearchParams(location.hash.slice(1)).get("token") : null;
let changeRequired = false;
let wantedRemember = false;
const pages = { POS: "/Pos", DASHBOARD: "/Dashboard", KHO: "/VanHanh" };
function show(text, error = false) {
  message.textContent = text;
  message.dataset.error = String(error);
}
async function post(action, payload, useXsrf = false) {
  const headers = { "Content-Type": "application/json" };
  if (useXsrf) {
    const key = "XSRF-TOKEN=";
    const cookie = document.cookie.split(";").map(x => x.trim()).find(x => x.startsWith(key));
    if (cookie) headers["X-XSRF-TOKEN"] = decodeURIComponent(cookie.slice(key.length));
  }
  const response = await fetch("/api/v1/auth/" + action, { method: "POST", headers, body: JSON.stringify(payload) });
  const result = response.headers.get("content-type")?.includes("json") ? await response.json() : {};
  if (!response.ok) throw new Error(result.message || (response.status === 429 ? "Bạn đã thử quá nhiều lần. Vui lòng thử lại sau." : "Yêu cầu chưa thực hiện được."));
  return result;
}
if (mode === "reset") {
  history.replaceState(null, "", location.pathname);
  if (!token) { form.hidden = true; show("Liên kết bị thiếu. Hãy yêu cầu liên kết mới từ trang Quên mật khẩu.", true); }
}
form.addEventListener("submit", async event => {
  event.preventDefault();
  button.disabled = true;
  show("Đang xử lý…");
  try {
    if (mode === "forgot") {
      const result = await post("forgot-password", { tenDangNhap: field("username").value.trim(), email: field("email").value.trim() });
      show(result.message);
    } else if (mode === "change") {
      await post("change-password", { matKhauHienTai: field("current").value, matKhauMoi: field("password").value, nhapLaiMatKhauMoi: field("confirm").value }, true);
      form.reset();
      show("Đã đổi mật khẩu.");
    } else if (mode === "reset") {
      await post("reset-password", { token, matKhauMoi: field("password").value, nhapLaiMatKhauMoi: field("confirm").value });
      form.reset();
      form.hidden = true;
      show("Đã đặt lại mật khẩu. Quay lại trang đăng nhập để sử dụng mật khẩu mới.");
    } else {
      const username = field("username").value.trim();
      const password = field("password").value;
      const remember = changeRequired ? wantedRemember : field("remember").checked;
      if (changeRequired) {
        await post("change-password", { matKhauHienTai: null, matKhauMoi: password, nhapLaiMatKhauMoi: field("confirm").value }, true);
        changeRequired = false;
        field("confirm-row").hidden = true;
        field("confirm").required = false;
        field("password-label").textContent = "Mật khẩu";
        field("login-options").hidden = false;
        field("remember").checked = remember;
        field("username").readOnly = false;
        button.textContent = "Đăng nhập";
      }
      const result = await post("login", { tenDangNhap: username, matKhau: password, ghiNho: remember });
      if (result.batBuocDoiMatKhau) {
        wantedRemember = field("remember").checked;
        changeRequired = true;
        field("username").readOnly = true;
        field("password").value = "";
        field("password").autocomplete = "new-password";
        field("password").minLength = 8;
        field("password-label").textContent = "Mật khẩu mới";
        field("confirm-row").hidden = false;
        field("confirm").required = true;
        field("login-options").hidden = true;
        button.textContent = "Đổi mật khẩu và đăng nhập";
        show("Đặt mật khẩu mới để hoàn tất lần đăng nhập đầu.");
        field("password").focus();
        return;
      }
      location.assign(pages[result.manHinhChinh] || "/");
    }
  } catch (error) {
    show(error.message || "Không kết nối được máy chủ.", true);
  } finally {
    button.disabled = false;
  }
});
