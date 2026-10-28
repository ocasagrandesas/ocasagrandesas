import { ApiError, getCsrfToken, postJson } from "./http.js";

const form = document.getElementById("login-form");
const errorBox = document.getElementById("login-error");
const submit = form.querySelector("button[type='submit']");

function messageFor(error) {
  if (!(error instanceof ApiError)) return "Não foi possível conectar. Verifique sua conexão e tente novamente.";
  if (error.status === 401) return "E-mail ou senha inválidos.";
  if (error.status === 429) return "Muitas tentativas. Aguarde um minuto e tente novamente.";
  return "Não foi possível entrar agora. Tente novamente.";
}

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  errorBox.textContent = "";
  submit.disabled = true;

  const data = new FormData(form);

  try {
    const csrfToken = await getCsrfToken();
    await postJson("/api/auth/login", { email: data.get("email"), password: data.get("password") }, csrfToken);
    window.location.assign("/admin");
  } catch (error) {
    errorBox.textContent = messageFor(error);
    form.elements.password.value = "";
    form.elements.password.focus();
  } finally {
    submit.disabled = false;
  }
});
