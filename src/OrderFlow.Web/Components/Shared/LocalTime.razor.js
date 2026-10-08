// Formata um instante ISO (UTC) no fuso horário do navegador, ex.: "08/10/2026 21:44 GMT-3".
export function formatLocal(iso) {
  return new Date(iso).toLocaleString("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    timeZoneName: "short",
  });
}
