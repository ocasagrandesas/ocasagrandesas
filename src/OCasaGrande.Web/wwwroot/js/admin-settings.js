import { ApiError, getCsrfToken, getJson, postJson, putJson } from "./http.js";

const list = document.getElementById("module-list");
const status = document.getElementById("settings-status");
const sessionEmail = document.getElementById("session-email");
const logoutButton = document.getElementById("logout-button");

let csrfToken = null;

function redirectIfSignedOut(error) {
  if (error instanceof ApiError && error.status === 401) {
    window.location.assign("/admin/entrar");
    return true;
  }
  return false;
}

function createModuleItem(module) {
  const item = document.createElement("li");
  item.className = "card module-item";

  const text = document.createElement("div");
  const title = document.createElement("h2");
  title.id = `module-${module.key}-title`;
  title.textContent = module.name;
  const description = document.createElement("p");
  description.textContent = module.description;
  const key = document.createElement("span");
  key.className = "module-item__key";
  key.textContent = `Identificador: ${module.key}`;
  text.append(title, description, key);

  const control = document.createElement("label");
  control.className = "module-item__control";
  const state = document.createElement("span");
  const toggle = document.createElement("input");
  toggle.type = "checkbox";
  toggle.className = "switch";
  toggle.setAttribute("role", "switch");
  toggle.setAttribute("aria-labelledby", title.id);
  toggle.checked = module.isEnabled;
  state.textContent = module.isEnabled ? "Ativo" : "Inativo";
  control.append(state, toggle);

  toggle.addEventListener("change", () => updateModule(module, toggle, state));

  item.append(text, control);
  return item;
}

async function updateModule(module, toggle, state) {
  const isEnabled = toggle.checked;
  toggle.disabled = true;
  status.textContent = "Salvando…";

  try {
    const updated = await putJson(`/api/admin/modules/${encodeURIComponent(module.key)}`, { isEnabled }, csrfToken);
    state.textContent = updated.isEnabled ? "Ativo" : "Inativo";
    status.textContent = `${updated.name} ${updated.isEnabled ? "ativado" : "desativado"}.`;
  } catch (error) {
    if (redirectIfSignedOut(error)) return;
    toggle.checked = !isEnabled;
    status.textContent = "Não foi possível salvar a alteração. Tente novamente.";
  } finally {
    toggle.disabled = false;
  }
}

async function load() {
  status.textContent = "Carregando módulos…";

  try {
    const [session, modules, token] = await Promise.all([
      getJson("/api/auth/session"),
      getJson("/api/admin/modules"),
      getCsrfToken(),
    ]);

    csrfToken = token;
    sessionEmail.textContent = session.email;
    list.replaceChildren(...modules.map(createModuleItem));
    status.textContent = modules.length === 0 ? "Nenhum módulo cadastrado." : "";
  } catch (error) {
    if (redirectIfSignedOut(error)) return;
    status.textContent = "Não foi possível carregar os módulos. Recarregue a página.";
  }
}

logoutButton.addEventListener("click", async () => {
  logoutButton.disabled = true;
  try {
    await postJson("/api/auth/logout", undefined, csrfToken);
  } finally {
    window.location.assign("/admin/entrar");
  }
});

load();
