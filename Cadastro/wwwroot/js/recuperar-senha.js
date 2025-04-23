import { mascara } from "./utils/mascara.js";
import { validacao } from "./utils/validacao.js";
import { showHidePassword, handleRequirementsList } from "./components/password.js";
import Popup from "./components/popup.js";
const form = document.querySelector("#cadastro");
const campos = form.querySelectorAll("[required]");

const fieldsets = document.querySelectorAll("fieldset");
const btnVoltar = document.querySelector("#btnVoltar");
const btnAvancar = document.querySelector("#btnAvancar");
const btnFinalizar = document.querySelector("#btnFinalizar");

btnVoltar.addEventListener("click", voltar);
btnAvancar.addEventListener("click", avancar);
btnFinalizar.addEventListener("click", () => popupConfirmacao.openPopup());

const popupConfirmacao = new Popup({
  titulo: "Atenção!",
  descricao: "Conferiu todos os dados?",
  botoes: [
    { label: "Não, quero alterar", classe: "btn--gray", onClick: () => popupConfirmacao.closePopup() },
    { label: "Sim, quero finalizar", classe: "btn--green", onClick: () => sendRequest() },
  ],
});
document.body.querySelector("main").appendChild(popupConfirmacao);

campos.forEach((campo) => {
  if (campo.name == "senha" || campo.name === "confirmacaoSenha") {
    showHidePassword(campo);
  }
  campo.addEventListener("input", (event) => {
    mascara(event);
    if (campo.name === "senha") {
      handleRequirementsList(campo);
    }
  });
  campo.addEventListener("invalid", async (event) => {
    event.preventDefault();
  });
});

form.addEventListener("submit", (event) => {
  event.preventDefault();
  popupConfirmacao.openPopup();
});

async function voltar() {
  const indexStepAtual = encontraIndex();

  if (indexStepAtual === 0) {
    return;
  }
  fieldsets[indexStepAtual].classList.remove("is-active");
  fieldsets[indexStepAtual - 1].classList.add("is-active");
}

async function avancar() {
  const indexStepAtual = encontraIndex();


    fieldsets[indexStepAtual].classList.remove("is-active");
    fieldsets[indexStepAtual + 1].classList.add("is-active");
  
}

function encontraIndex() {
  let indexStepAtual = 0;

  fieldsets.forEach((el, index) => {
    if (el.classList.contains("is-active")) {
      indexStepAtual = index;
    }
  });

  return indexStepAtual;
}

async function validaCampos(indexStepAtual) {
  const arrCampos = fieldsets[indexStepAtual].querySelectorAll("[required]");

  const validacoes = Array.from(arrCampos).map((campo) => validacao(campo));
  const resultadosValidacao = await Promise.all(validacoes);

  if (resultadosValidacao.includes(true)) {
    return false;
  }

  return true;
}

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

function getTokenFromUrl() {
    const urlParams = new URLSearchParams(window.location.search);
    const token = urlParams.get("token");
    if (!token) {
        console.error("Token não encontrado na URL.");
        showError("Token de redefinição de senha não encontrado. Por favor, solicite um novo link.");
        return null;
    }
    return token;
}

async function sendRequest() {
  try {
    popupConfirmacao.closePopup();
    document.body.classList.add("loading");

      const token = getTokenFromUrl();

      const formData = new FormData(form);
      const data = Object.fromEntries(formData);
      data["cpf"] = data["cpf"].replace(/\D/g, "");
      data["Token"] = token;

      data["forcarErro"] = false;
      console.log(data);

      const response = await fetch("https://localhost:7011/api/v1/Auth/redefinir-senha", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });

    document.body.classList.remove("loading");

    if (response.ok) {
      const popupSucesso = new Popup({
        titulo: "Tudo certo!",
        descricao: "Sua senha foi redefinida com sucesso.",
        status: "sucesso",
        funcaoRedirecionamento: () => window.location.assign("./login.html"),
      });
      document.body.querySelector("main").appendChild(popupSucesso);
      popupSucesso.openPopup();
      window.location.assign("./login.html")
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