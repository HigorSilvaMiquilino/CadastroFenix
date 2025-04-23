import { mascara } from "./utils/mascara.js";
import { validacao} from "./utils/validacao.js";
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

const popupConfirmacao = new Popup({
    titulo: "Atenção!",
    descricao: "Conferiu todos os dados?",
    botoes: [
        { label: "Não, quero alterar", classe: "btn--gray", onClick: () => popupConfirmacao.closePopup() },
        { label: "Sim, quero finalizar", classe: "btn--green", onClick: async () => sendRequest()  }
    ],
});
document.body.querySelector("main").appendChild(popupConfirmacao);

window.onloadTurnstileCallback = function () {
    turnstile.render("#example-container-index", {
        sitekey: "0x4AAAAAABNn1hyZ5p_YGpRs",
        callback: function (token) {
            document.getElementById("CfTurnstileResponse").value = token;
            console.log(`Challenge Success ${token}`);
        },
    });
};

campos.forEach((campo) => {
    if (campo.name == "senha" || campo.name === "confirmacaoSenha") {
        showHidePassword(campo);
    }
    campo.addEventListener("blur", validacao);
    campo.addEventListener("input", (event) => {
        mascara(event);
        if (campo.name === "senha") {
            handleRequirementsList(campo);
        }
    });
    campo.addEventListener("invalid", async (event) => {
        event.preventDefault();
        await validacao(campo);
    });
});


form.addEventListener("submit", async (event) => {
    event.preventDefault();


    const indexStepAtual = encontraIndex();

    const ehCaamposValidos = validaCampos(indexStepAtual);

    if (!ehCaamposValidos) {
        return;
    }

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
    const ehCaamposValidos = await validaCampos(indexStepAtual);

    if (ehCaamposValidos) {
        if (indexStepAtual === fieldsets.length - 1) {
            return;
        }

        fieldsets[indexStepAtual].classList.remove("is-active");
        fieldsets[indexStepAtual + 1].classList.add("is-active");
    }
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


document.addEventListener("DOMContentLoaded", function () {
    const svgBox = document.querySelector(".svgBox");
    const svgLogo = document.querySelector(".svgLogo");
    const formLogin = document.querySelector(".section__login-form");

    svgBox.classList.add("animate__fadeIn");

    setTimeout(() => {
        svgLogo.classList.replace("hidden", "animate__fadeInDown");
        formLogin.classList.replace("hidden", "animate__fadeInUp");
    }, 1000);
});

async function sendRequest() {
    popupConfirmacao.closePopup();

    const turnstileToken = document.getElementById("CfTurnstileResponse").value;
    if (!turnstileToken) {
        showError("Por favor, complete a verificação do CAPTCHA.");
        return;
    }

    try {
        const formData = {
            cpf: form.querySelector("#cpf").value,
            nomeCompleto: form.querySelector("#nomeCompleto").value,
            dataNascimento: form.querySelector("#dataNascimento").value,
            genero: form.querySelector("#genero").value,
            telefone: form.querySelector("#telefone").value,
            cep: form.querySelector("#cep").value,
            logradouro: form.querySelector("#endereco").value,
            numero: form.querySelector("#numero").value,
            bairro: form.querySelector("#bairro").value,
            estado: form.querySelector("#estado").value,
            cidade: form.querySelector("#cidade").value,
            email: form.querySelector("#email").value,
            confirmacaoEmail: form.querySelector("#confirmacaoEmail").value,
            senha: form.querySelector("#senha").value,
            confirmacaoSenha: form.querySelector("#confirmacaoSenha").value,
            aceiteTermos: form.querySelector("#aceite-1").checked,
            CfTurnstileResponse: turnstileToken
        };

        const response = await fetch("https://localhost:7011/api/v1/Cadastro/Cadastrar", {
            method: "Post",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(formData),
        });

        if (response.ok) {
            const data = await response.json();
            localStorage.setItem("nome", data.Metadata.nome);
            window.location.assign("./sucesso-cadastro.html") 
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
            let errorMessage = errorData.Message || "Erro ao cadastrar usuário.";

            if (errorData.Errors && Object.keys(errorData.Errors).length > 0) {
                errorMessage = "Por favor, corrija os seguintes erros:<br><br>";
                errorMessage += '<ul style="list-style-type: none; padding: 0; margin: 0; text-align: left;">';
                for (const [field, Message] of Object.entries(errorData.Errors)) {
                    errorMessage += `<li style="margin-bottom: 0.5rem; line-height: 1.4;">• <strong style="color: #fff;">${field}:</strong> <strong style="color: 	#ff0000;">${Message}:</strong></li>`;
                    const inputField = form.querySelector(`#${field}`);
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