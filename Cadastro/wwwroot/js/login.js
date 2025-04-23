import { mascara } from "./utils/mascara.js";
import { validacao } from "./utils/validacao.js";
import { showHidePassword } from "./components/password.js";
import Popup from "./components/popup.js";
const form = document.querySelector("#login");
const campos = form.querySelectorAll("[required]");

campos.forEach((campo) => {
  if (campo.name == "senha") {
    showHidePassword(campo);
  }

  campo.addEventListener("input", mascara);
  campo.addEventListener("invalid", async (event) => {
    event.preventDefault();
    await validacao(campo);
  });
});

window.onloadTurnstileCallback = function () {
    turnstile.render("#example-container", {
        sitekey: "0x4AAAAAABNn1hyZ5p_YGpRs",
        callback: function (token) {
            document.getElementById("CfTurnstileResponse").value = token;
            console.log(`Challenge Success ${token}`);
        },
    });
};

form.addEventListener("submit", (event) => {
    event.preventDefault();
    const turnstileToken = document.getElementById("CfTurnstileResponse").value;
    if (!turnstileToken) {
        showError("Por favor, complete a verificação do CAPTCHA.");
        return;
    }
  sendRequest();
});

let popupErro = null;
function showError(txt = "Parece que houve um erro com o seu cadastro, aguarde e tente novamente mais tarde.") {
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
      console.log(data);
      data["forcarErro"] = false;
      
      console.log(data);

      const response = await fetch("https://localhost:7011/api/v1/Auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });

    document.body.classList.remove("loading");

      if (response.ok) {
          const registro = {
              conectado: true,
          };
          localStorage.setItem("login", JSON.stringify(registro));
          const data = await response.json();
          localStorage.setItem("nome", data.Metadata.nome);
          window.location.assign("./area-logada.html");
          return;
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
          popupConfirmacao.closePopup();
          showError();
          return;
      } else {
          const errorData = await response.json();
          console.log(errorData);
          const errorMessage = errorData.Message || "Erro desconhecido.";
          if (errorData.metadata?.travadaPelosSegundosRestantes) {
              errorMessage += ` Restam ${errorData.metadata.travadaPelosSegundosRestantes} segundos para desbloquear.`;
          }
          showError(errorMessage);
          return;
      }
  } catch (error) {
    console.error("Erro na requisição:", error);
  }

  showError();
}

