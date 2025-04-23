import { mascara } from "./utils/mascara.js";
import { validacao } from "./utils/validacao.js";
import Popup from "./components/popup.js";
const form = document.querySelector("#form");
const campo = form.querySelector("[required]");

campo.addEventListener("input", mascara);
campo.addEventListener("invalid", async (event) => {
  event.preventDefault();
  await validacao(campo);
});

form.addEventListener("submit", (event) => {
  event.preventDefault();
  sendRequest();
});

let popupErro = null;
function showError(txt = "Parece que houve um erro com a sua solicitação, aguarde e tente novamente mais tarde.") {
  if (popupErro == null) {
    popupErro = new Popup({
      titulo: "Ocorreu algo inesperado.",
      descricao: txt,
      status: "erro",
      botoes: [{ label: "Entendi", classe: "btn--red", onClick: () => popupErro.closePopup() }],
      funcaoRedirecionamento: () => window.location.reload(),
    });
    document.body.querySelector("main").appendChild(popupErro);
  } else popupErro.setDescricao(txt);

  popupErro.openPopup();
}

async function sendRequest() {
  try {
    document.body.classList.add("loading");

    const formData = new FormData(form);
    const data = Object.fromEntries(formData);

    data["forcarErro"] = false;

      const response = await fetch("https://localhost:7011/api/v1/Auth/esqueci-senha", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });

    document.body.classList.remove("loading");

      if (response.ok) {
      const data = await response.json();   
      let email = campo.value.split(" ")[0];
      const popupEnviado = new Popup({
        titulo: data.Message,
        descricao: `Você receberá o link no seguinte endereço de e-mail: <br><br> <b>${email}</b>. <br><br> Caso não a tenha recebido, confira sua caixa de SPAM.`,
        status: "sucesso",
        botoes: [{ label: "Entendi", classe: "btn--green", onClick: () => popupEnviado.closePopup() }],
        funcaoRedirecionamento: () => window.location.assign("./login.html"),
      });
      document.body.querySelector("main").appendChild(popupEnviado);
      popupEnviado.openPopup();
    } else if (response.status === 429) {
        const errorData = await response.json();
        const retryAfter = errorData.retryAfter || 60;

        const popupRateLimit = new Popup({
            titulo: "Muitas Requisições!",
            descricao: errorData.message || `Você atingiu o limite de requisições. Tente novamente em ${retryAfter} segundos.`,
            status: "erro",
            botoes: [{ label: "Entendi", classe: "btn--red", onClick: () => popupRateLimit.closePopup() }]
        });
        document.body.querySelector("main").appendChild(popupRateLimit);
        popupRateLimit.openPopup();

        btnFinalizar.disabled = true;
        setTimeout(() => {
            btnFinalizar.disabled = false;
        }, retryAfter * 1000);

        return;
    } else if (response.status === 409) {
      showError();
      return;
    } else {
        const errorData = await response.json();
        console.log(errorData);
        const errorMessage = errorData.Message || "Erro desconhecido.";
        showError(errorMessage);
        return;
    }
    campo.classList.remove("is-valid");
    form.reset();
  } catch (error) {
    console.error("Erro na requisição:", error);
    showError();
  }
}