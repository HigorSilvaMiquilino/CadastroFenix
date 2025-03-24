import { mascara } from "./utils/mascara.js";
import { validacao } from "./utils/validacao.js";
import { showHidePassword, handleRequirementsList } from "./components/password.js";
import Popup from "./components/popup.js";
const form = document.querySelector("#cadastro");
const campos = form.querySelectorAll("[required]");

const fieldsets = document.querySelectorAll("fieldset");
const btnVoltar = document.querySelector("#btnVoltar");
const btnAvancar = document.querySelector("#btnAvancar");

btnVoltar.addEventListener("click", voltar);
btnAvancar.addEventListener("click", avancar);

const popupConfirmacao = new Popup({
    titulo: "Atenção!",
    descricao: "Conferiu todos os dados?",
    botoes: [
        { label: "Não, quero alterar", classe: "btn--gray", onClick: () => popupConfirmacao.closePopup() },
        {
            label: "Sim, quero finalizar",
            classe: "btn--green",
            onClick: async () => {

                popupConfirmacao.closePopup();

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
                        aceiteTermos: form.querySelector("#aceite-1").checked
                    };


                    const response = await fetch("https://localhost:7011/api/v1/Cadastro/Cadastrar", {
                        method: "POST",
                        headers: {
                            "Content-Type": "application/json"
                        },
                        body: JSON.stringify(formData)
                    });

                    if (response.ok) {
                        window.location.assign("./sucesso-cadastro.html")
                    } else {
                        const errorData = await response.json();
                        let errorMessage = errorData.message || "Erro ao cadastrar usuário.";

                        if (errorData.errors && Object.keys(errorData.errors).length > 0) {
                            errorMessage = "Por favor, corrija os seguintes erros:<br>";
                            for (const [field, message] of Object.entries(errorData.errors)) {
                                errorMessage += `${field}: ${message}<br>`;
                                const inputField = form.querySelector(`#${field}`);
                                if (inputField) {
                                    const errorSpan = inputField.closest(".input__container").querySelector(".input__error-msg");
                                    errorSpan.textContent = message;
                                    errorSpan.style.display = "block";
                                }
                            }
                        }

                        throw new Error(errorMessage);
                    }
                } catch (error) {
                    const popupErro = new Popup({
                        titulo: "Ocorreu algo inesperado.",
                        descricao: error.message || "Parece que houve um erro com o seu cadastro, aguarde e tente novamente mais tarde.",
                        status: "erro",
                        botoes: [{ label: "Entendi", classe: "btn--red", onClick: () => popupErro.closePopup() }],
                    });
                    document.body.querySelector("main").appendChild(popupErro);
                    popupErro.openPopup();
                }
            }
        }
    ],
});
document.body.querySelector("main").appendChild(popupConfirmacao);


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