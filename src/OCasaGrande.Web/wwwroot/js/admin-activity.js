import { ApiError, getJson } from "./http.js";

const list = document.getElementById("activity-list");
const status = document.getElementById("activity-status");

const EVENT_LABELS = {
  LoginSucceeded: "Login realizado",
  LoginFailed: "Login recusado",
  Logout: "Saída",
  ModuleUpdated: "Módulo alterado",
};

const dateFormat = new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "medium" });

function createRow(event) {
  const row = document.createElement("tr");
  const values = [
    dateFormat.format(new Date(event.occurredAt)),
    EVENT_LABELS[event.type] ?? event.type,
    event.actor ?? "—",
    event.ipAddress ?? "—",
    event.detail ?? "—",
  ];

  for (const value of values) {
    const cell = document.createElement("td");
    cell.textContent = value;
    row.append(cell);
  }

  return row;
}

async function load() {
  status.textContent = "Carregando atividade…";

  try {
    const events = await getJson("/api/admin/security-events");
    list.replaceChildren(...events.map(createRow));
    status.textContent = events.length === 0 ? "Nenhuma atividade registrada." : "";
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) return;
    status.textContent = "Não foi possível carregar a atividade recente.";
  }
}

load();
