import { mascara } from "./utils/mascara.js";
import { validacao } from "./utils/validacao.js";
import ProdutoForm from "./components/form-produtos.js";
import Popup from "./components/popup.js";

const formDadosCupom = document.querySelector("#dadosCupom");
const campos = document.querySelectorAll("[required]");
const btnSubmit = document.querySelector("#btnSubmit");

document.addEventListener('DOMContentLoaded', async () => {
    try {
        const response = await fetchWithAuth('https://localhost:7011/api/v1/Auth/check-auth');
        if (!response.ok) {
            window.location.assign('./login.html');
        }
    } catch (error) {
        window.location.assign('./login.html');
    }
});

campos.forEach((campo) => {
    campo.addEventListener("blur", validacao);
    campo.addEventListener("input", mascara);
    campo.addEventListener("invalid", async (event) => {
        event.preventDefault();
        await validacao(campo);
    });
});

const popupConfirmacao = new Popup({
    titulo: "Atenção!",
    descricao: "Conferiu todos os dados?",
    botoes: [
        { label: "Não, quero alterar", classe: "btn--gray", onClick: () => popupConfirmacao.closePopup() },
        { label: "Sim, quero finalizar", classe: "btn--green", onClick: async () => sendRequest() },
    ],
});
document.body.querySelector("main").appendChild(popupConfirmacao);

const formProdutos = document.querySelector('produto-form');
formProdutos.addEventListener('atualizacao-produtos', (event) => {
    const inputQuantidade = formDadosCupom.querySelector('#quantidadeTotal');
    const { quantidadeTotal } = event.detail;
    inputQuantidade.value = parseInt(quantidadeTotal);

    validacao(inputQuantidade);
});

btnSubmit.addEventListener("click", async (event) => {
    event.preventDefault();
    const validacao = await validaCampos(document.querySelector("#dadosCupom"));

    if (validacao) {
        popupConfirmacao.openPopup();
    } else {
        window.scrollTo(0, 0);
    }
});

async function validaCampos(formulario) {
    const camposFormulario = formulario.querySelectorAll("[required]");

    const validacoes = Array.from(camposFormulario).map((campo) => validacao(campo));
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
            botoes: [{
                label: "Entendi",
                classe: "btn--red",
                onClick: () => {
                    popupErro.closePopup();
                    if (txt.includes("sessão expirou") || txt.includes("autenticação") || txt.includes("não autorizado")) {
                        window.location.assign("./login.html");
                    }
                }
            }],
            funcaoRedirecionamento: () => window.location.reload(),
        });
        document.body.querySelector("main").appendChild(popupErro);
    } else {
        popupErro.setDescricao(txt);
    }

    popupErro.openPopup();
}

async function refreshToken() {
    const response = await fetch("https://localhost:7011/api/v1/auth/refresh-token", {
        method: "POST",
        credentials: "include"
    });

    console.log("Refresh token response status:", response.status);
    console.log("Refresh token response headers:", [...response.headers.entries()]);

    const responseText = await response.text();
    console.log("Refresh token response body (raw):", responseText);

    if (!response.ok) {
        let errorData = null;
        try {
            errorData = responseText ? JSON.parse(responseText) : null;
        } catch (jsonError) {
            console.error("Failed to parse refresh token response as JSON:", jsonError);
        }

        const errorMessage = errorData?.Message || "Sua sessão expirou. Por favor, faça login novamente.";
        throw new Error(errorMessage);
    }

    try {
        return JSON.parse(responseText);
    } catch (jsonError) {
        console.error("Failed to parse refresh token response as JSON:", jsonError);
        throw new Error("Resposta do servidor para atualização de token não é um JSON válido.");
    }
}

async function fetchWithAuth(url, options = {}) {
    let response = await fetch(url, {
        ...options,
        credentials: "include"
    });

    if (response.status === 401) {
        try {
            await refreshToken();
            response = await fetch(url, {
                ...options,
                credentials: "include"
            });
        } catch (error) {
            throw new Error(error.message || "Falha ao atualizar token de autenticação.");
        }
    }

    return response;
}

async function sendRequest() {
    try {
        popupConfirmacao.closePopup();
        document.body.classList.add("loading");

        const formData = new FormData(formDadosCupom);
        const data = Object.fromEntries(formData);

        const rows = document.querySelectorAll("tr[data-id]");
        const produtos = [];

        Array.from(rows).forEach((row) => {
            produtos.push({
                id: row.getAttribute("data-id"),
                descricao: row.getAttribute("data-descricao"),
                quantidade: row.getAttribute("data-qtd"),
                valor: row.getAttribute("data-valor"),
            });
        });

        data["produtos"] = produtos;
        data["forcarErro"] = false;

        console.log("Request data:", data);

        const response = await fetchWithAuth("https://localhost:7011/api/v1/Cupom/cadastrar-cupom", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(data),
            credentials: "include"
        });

        document.body.classList.remove("loading");

        if (response.ok) {
            window.location.assign("./cupom-cadastrado.html");
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
        } else {
            const errorData = await response.json();
            console.log("Error data:", errorData);
            let errorMessage = errorData.Message || "Erro ao cadastrar usuário.";

            if (errorData.Errors && Object.keys(errorData.Errors).length > 0) {
                errorMessage = "Por favor, corrija os seguintes erros:<br><br>";
                errorMessage += '<ul style="list-style-type: none; padding: 0; margin: 0; text-align: left;">';
                for (const [field, Message] of Object.entries(errorData.Errors)) {
                    errorMessage += `<li style="margin-bottom: 0.5rem; line-height: 1.4;">• <strong style="color: #fff;">${field}:</strong> <strong style="color: 	#ff0000;">${Message}:</strong></li>`;
                    const inputField = formDadosCupom.querySelector(`#${field}`);
                    if (inputField) {
                        const errorSpan = inputField.closest(".input__container").querySelector(".input__error-msg");
                        errorSpan.textContent = Message;
                        errorSpan.style.display = "block";
                    }
                }
                errorMessage += "</ul>";
            }

            throw new Error(errorMessage);
        }
    } catch (error) {
        const popupErro = new Popup({
            titulo: "Ocorreu algo inesperado.",
            descricao: error || "Parece que houve um erro com o seu cadastro, aguarde e tente novamente mais tarde.",
            status: "erro",
            botoes: [{ label: "Entendi", classe: "btn--red", onClick: () => popupErro.closePopup() }],
        });
        document.body.querySelector("main").appendChild(popupErro);
        popupErro.openPopup();
    }
}
