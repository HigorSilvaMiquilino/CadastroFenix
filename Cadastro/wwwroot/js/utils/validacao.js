import { parseData, ehDataValida, ehMaiorDeIdade, ehDataFutura } from "./parsedata.js";
import { getCEP } from "./getCep.js";
import { getCidade, setCidade } from "./getCidade.js";
import  Popup  from "../components/popup.js";

const btnFinalizar = document.querySelector("#btnFinalizar");


export function validacao(elemento) {

    let campo = elemento;

    if (elemento instanceof Event) {
        campo = elemento.target;
    }

    const valida = {
        cpf: {
            funcaoValidacao: validaCPF,
            mensagensErro: {},
        },
        nomeCompleto: {
            funcaoValidacao: validaNomeCompleto,
            mensagensErro: {},
        },
        dataNascimento: {
            funcaoValidacao: validaDataNascimento,
            mensagensErro: {},
        },
        genero: {
            funcaoValidacao: null,
            mensagensErro: {},
        },
        telefone: {
            funcaoValidacao: validaTelefone,
            mensagensErro: {
                tooShort: "Insira um número de telefone válido",
            },
        },
        cep: {
            funcaoValidacao: validaCEP,
            mensagensErro: {},
        },
        endereco: {
            funcaoValidacao: null,
            mensagensErro: {},
        },
        numero: {
            funcaoValidacao: null,
            mensagensErro: {},
        },
        bairro: {
            funcaoValidacao: null,
            mensagensErro: {},
        },
        estado: {
            funcaoValidacao: validaEstado,
            mensagensErro: {},
        },
        cidade: {
            funcaoValidacao: validaCidade,
            mensagensErro: {},
        },
        email: {
            funcaoValidacao: validaEmail,
            mensagensErro: {
                typeMismatch: "Insira um endereço de e-mail válido",
            },
        },
        confirmacaoEmail: {
            funcaoValidacao: validaConfirmacaoEmail,
            mensagensErro: {
                typeMismatch: "Insira um endereço de e-mail válido.",
            },
        },
        senha: {
            funcaoValidacao: validaSenha,
            mensagensErro: {
                tooShort: "A senha deve conter 8 caracteres com ao menos um(a): letra maiúscula, letra minúscula, número, caractere especial",
            },
        },
        confirmacaoSenha: {
            funcaoValidacao: validaConfirmacaoSenha,
            mensagensErro: {},
        },

    };

    if (valida[campo.name]) {
        if (!valida[campo.name].mensagensErro.valueMissing) {
            valida[campo.name].mensagensErro.valueMissing = "Campo obrigatório";
        }

        !valida[campo.name].mostradorErro ? (valida[campo.name].mostradorErro = campo.parentElement.parentElement.querySelector(".input__error-msg")) : "";

        return encontraErros(campo, valida[campo.name]);
    }

}

async function encontraErros(campo, obj) {
    if (obj.funcaoValidacao) {
        await obj.funcaoValidacao(campo);
    }

    const listaErros = campo.validity;

    for (const erro in listaErros) {
        erro == "customError" ? (obj.mensagensErro[erro] = campo.validationMessage) : "";
        if (listaErros[erro] === true && erro !== "valid") {
            obj.mostradorErro.textContent = obj.mensagensErro[erro];
            obj.mostradorErro.classList.add("is-visible");
            campo.classList.remove("is-valid");

            return true;
        }
    }
    campo.classList.add("is-valid");
    obj.mostradorErro.classList.remove("is-visible");
    return false;
}

async function validaCPF(campo) {
    const cpf = campo.value.replace(/\.|-/g, "");

    const temNumerosRepetidos = (cpf) => {
        return ["00000000000", "11111111111", "22222222222", "33333333333", "44444444444", "55555555555", "66666666666", "77777777777", "88888888888", "99999999999"].includes(cpf);
    };

    function validaDigito(cpf, digitoVerificador) {
        const posicao = digitoVerificador === 1 ? 9 : 10;

        let soma = 0;
        for (let i = 0; i < posicao; i++) {
            let multiplicador = posicao + 1 - i;
            soma += cpf[i] * multiplicador;
        }

        const resto = (soma * 10) % 11;
        if (resto === 10 || resto === 11) {
            return parseInt(cpf[posicao]) === 0;
        }

        return parseInt(cpf[posicao]) === resto;
    }

    if (cpf.length != 11 || temNumerosRepetidos(cpf) || !validaDigito(cpf, 1) || !validaDigito(cpf, 2)) {
        return campo.setCustomValidity("Insira um CPF válido.");
    }

    let response = await fetch(`https://localhost:7011/api/v1/cadastro/verificar-cpf?cpf=${encodeURIComponent(cpf)}`, {
        method: "GET",
        headers: {
            "Content-Type": "application/json"
        }
    });
    
    if (response.status === 429) {
        const errorData = await response.json();
        let retryAfter = errorData.retryAfter || 60; 

        const popupRateLimit = new Popup({
            titulo: "Muitas Requisições!",
            descricao: errorData.message || `Você fez muitas requisições. Por favor, aguarde ${retryAfter} segundos antes de tentar novamente.`,
            status: "erro",
            botoes: [{ label: "Entendi", classe: "btn--red"}]
        });
        document.body.querySelector("main").appendChild(popupRateLimit);
        popupRateLimit.openPopup();


        btnFinalizar.disabled = true;

        const countdownInterval = setInterval(() => {
            retryAfter -= 1;

            if (retryAfter <= 0) {
                clearInterval(countdownInterval);
                btnFinalizar.disabled = false;
                campo.setCustomValidity("");
                popupRateLimit.closePopup();
                return;
            }

            popupRateLimit.setDescricao(`Você fez muitas requisições. Por favor, aguarde ${retryAfter} segundos antes de tentar novamente.`);

        }, 1000);

        return;
    }
    
    let data = await response.json();
    return data.success ? campo.setCustomValidity("") : campo.setCustomValidity(data.message);

    return campo.setCustomValidity("");
}

function validaNome(campo) {
    const caracteres = campo.value.split(/\s+/);
    const comprimentoNome = caracteres[0].length;

    if (comprimentoNome < 2) {
        return false;
    }

    return true;
}

function validaNomeCompleto(campo) {
    if (!validaNome(campo)) {
        return campo.setCustomValidity("O nome precisa ter no minímo 2 caracteres");
    }

    const palavras = campo.value.split(/\s+/);

    if (!palavras[1] || palavras[1].length < 2) {
        campo.setCustomValidity("Insira o nome completo");
        return;
    }

    campo.value = palavras.join(" ");
    return campo.setCustomValidity("");
}

function validaDataNascimento(campo) {
    const match = campo.value.match(/^(\d{2})\/(\d{2})\/(\d{4})$/);

    if (match === null) {
        campo.setCustomValidity("Insira uma data válida.");
        return;
    }

    const { dia, mes, ano, data } = parseData(match);

    if (!ehDataValida(dia, mes, ano, data)) {
        return campo.setCustomValidity("Insira uma data válida.");
    } else if (ehDataFutura(dia, mes, ano)) {
        return campo.setCustomValidity("Não é permitido inserir data futura.");
    } else if (!ehMaiorDeIdade(data)) {
        return campo.setCustomValidity("É necessário ter ao menos 18 anos para participar.");
    }

    return campo.setCustomValidity("");
}

async function validaTelefone(campo) {
    const match = campo.value.match(/^\(([1-9]{2})\)\s9?\d{4}-\d{4}$/);

    if (!match) {
        campo.setCustomValidity("Insira um número de telefone válido");
        return;
    }

    const codigosDeArea = [
        "68",
        "82",
        "96",
        "92",
        "97",
        "71",
        "73",
        "74",
        "75",
        "77",
        "85",
        "88",
        "61",
        "27",
        "28",
        "62",
        "64",
        "98",
        "99",
        "65",
        "66",
        "67",
        "31",
        "32",
        "33",
        "34",
        "35",
        "37",
        "38",
        "91",
        "93",
        "94",
        "83",
        "41",
        "42",
        "43",
        "44",
        "45",
        "46",
        "81",
        "87",
        "86",
        "89",
        "21",
        "22",
        "24",
        "84",
        "51",
        "53",
        "54",
        "55",
        "69",
        "95",
        "47",
        "48",
        "49",
        "11",
        "12",
        "13",
        "14",
        "15",
        "16",
        "17",
        "18",
        "19",
        "79",
        "63",
    ];

    const codigo = match[1];

    if (!codigosDeArea.includes(codigo)) {
        campo.setCustomValidity("Insira um DDD válido");
        return;
    }

    let response = await fetch(`https://localhost:7011/api/v1/cadastro/verificar-telefone?telefone=${encodeURIComponent(campo.value)}`, {
        method: "GET",
        headers: {
            "Content-Type": "application/json"
        }
    });

    if (response.status === 429) {
        const errorData = await response.json();
        let retryAfter = errorData.retryAfter || 60;

        const popupRateLimit = new Popup({
            titulo: "Muitas Requisições!",
            descricao: errorData.message || `Você fez muitas requisições. Por favor, aguarde ${retryAfter} segundos antes de tentar novamente.`,
            status: "erro",
            botoes: [{ label: "Entendi", classe: "btn--red" }]
        });
        document.body.querySelector("main").appendChild(popupRateLimit);
        popupRateLimit.openPopup();


        btnFinalizar.disabled = true;

        const countdownInterval = setInterval(() => {
            retryAfter -= 1;

            if (retryAfter <= 0) {
                clearInterval(countdownInterval);
                btnFinalizar.disabled = false;
                campo.setCustomValidity("");
                popupRateLimit.closePopup();
                return;
            }

            popupRateLimit.setDescricao(`Você fez muitas requisições. Por favor, aguarde ${retryAfter} segundos antes de tentar novamente.`);

        }, 1000);

        return;
    }

    let data = await response.json();
    return data.success ? campo.setCustomValidity("") : campo.setCustomValidity(data.message);

    return campo.setCustomValidity("");
}

function validaCelular(campo) {
    const match = campo.value.match(/^\(([1-9]\d)\)\s9\d{4}-\d{4}$/);

    if (!match) {
        campo.setCustomValidity("Insira um número de celular válido");
        return;
    }

    const codigosDeArea = [
        "68",
        "82",
        "96",
        "92",
        "97",
        "71",
        "73",
        "74",
        "75",
        "77",
        "85",
        "88",
        "61",
        "27",
        "28",
        "62",
        "64",
        "98",
        "99",
        "65",
        "66",
        "67",
        "31",
        "32",
        "33",
        "34",
        "35",
        "37",
        "38",
        "91",
        "93",
        "94",
        "83",
        "41",
        "42",
        "43",
        "44",
        "45",
        "46",
        "81",
        "87",
        "86",
        "89",
        "21",
        "22",
        "24",
        "84",
        "51",
        "53",
        "54",
        "55",
        "69",
        "95",
        "47",
        "48",
        "49",
        "11",
        "12",
        "13",
        "14",
        "15",
        "16",
        "17",
        "18",
        "19",
        "79",
        "63",
    ];

    const codigo = match[1];

    if (!codigosDeArea.includes(codigo)) {
        campo.setCustomValidity("Insira um DDD válido");
        return;
    }

    return campo.setCustomValidity("");
}

async function validaEmail(campo) {
    const regex = /^(?:[\p{L}0-9]+(?:[-_\+\.][\p{L}0-9]){0,})+@[a-z0-9]{2,}\.[a-z0-9]{2,}(?:\.[a-z0-9]{1,}){0,}$/giu;

    if (!regex.test(campo.value)) {
        return campo.setCustomValidity("Insira um endereço de e-mail válido");
    }

    let response = await fetch(`https://localhost:7011/api/v1/cadastro/verificar-email?email=${encodeURIComponent(campo.value)}&confirmacao=${encodeURIComponent(campo.value)}`, {
        method: "GET",
        headers: {
            "Content-Type": "application/json"
        }
    });

    if (response.status === 429) {
        const errorData = await response.json();
        let retryAfter = errorData.retryAfter || 60;

        const popupRateLimit = new Popup({
            titulo: "Muitas Requisições!",
            descricao: errorData.message || `Você fez muitas requisições. Por favor, aguarde ${retryAfter} segundos antes de tentar novamente.`,
            status: "erro",
            botoes: [{ label: "Entendi", classe: "btn--red"}]
        });
        document.body.querySelector("main").appendChild(popupRateLimit);
        popupRateLimit.openPopup();

        btnFinalizar.disabled = true;

        const countdownInterval = setInterval(() => {
            retryAfter -= 1; 

            if (retryAfter <= 0) {
                clearInterval(countdownInterval);
                btnFinalizar.disabled = false;
                campo.setCustomValidity(""); 
                popupRateLimit.closePopup();
                return;
            }

            popupRateLimit.setDescricao(`Você fez muitas requisições. Por favor, aguarde ${retryAfter} segundos antes de tentar novamente.`);

        }, 1000); 

        return;
    }

    let data = await response.json();
    return data.success ? campo.setCustomValidity("") : campo.setCustomValidity(data.message);

    return campo.setCustomValidity("");
}

function validaConfirmacaoEmail(campo) {
    const inputEmail = document.querySelector("input#email");
    const ehIgual = inputEmail.value === campo.value;

    if (!ehIgual) {
        return campo.setCustomValidity("Os e-mails não correspondem.");
    }

    return campo.setCustomValidity("");
}

function validaSenha(campo) {
    let validaSenha = /(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?])[a-zA-Z0-9!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]{8,}/g;

    if (campo.value == "") {
        return campo.setCustomValidity("Campo obrigatório");
    }

    if (!validaSenha.test(campo.value)) {
        return campo.setCustomValidity("A senha deve conter 8 caracteres com ao menos um(a): letra maiúscula, letra minúscula, dois números e caractere especial");
    }

    return campo.setCustomValidity("");
}

function validaConfirmacaoSenha(campo) {
    const inputSenha = document.querySelector("#senha");
    const ehIgual = inputSenha.value === campo.value;

    if (!ehIgual) {
        return campo.setCustomValidity("As senhas não correspondem");
    }
    return campo.setCustomValidity("");
}

async function validaCEP(campo) {

    const cep = campo.value.replace(/\D/g, "");

    if (!cep.match(/\d{8}/)) {
        return campo.setCustomValidity("Insira um CEP válido.");
    }

    let { erro, uf, municipio, bairro, logradouro } = await getCEP(cep);

    if (erro) {
        return campo.setCustomValidity("O CEP inserido é inválido");
    }

    campo.setCustomValidity("");
    const estado = document.querySelector('[name="estado"]');
    const cidade = document.querySelector('[name="cidade"]');
    const bairroInput = document.querySelector('[name="bairro"]');
    const endereco = document.querySelector('[name="endereco"]');

    const listaCidades = await getCidade(uf);
    setCidade(cidade, listaCidades);

    estado.value = uf;
    validacao(estado);

    if (municipio !== "") {
        const municipioNormalizado = normalizarTexto(municipio);

        for (let option of cidade.options) {
            if (normalizarTexto(option.value) === municipioNormalizado) {
                cidade.value = option.value;
                break;
            }
        }
    }
    validacao(cidade);

    bairroInput.value = bairro;
    validacao(bairroInput);

    endereco.value = logradouro;
    validacao(endereco);
}

async function validaEstado(campo) {
    const estado = campo.value;
    const cidade = document.querySelector('[name="cidade"]');

    try {
        const cep = document.querySelector('[name="cep"]');
        if (cep.value) {
            const { uf } = await getCEP(cep.value);
            uf !== estado ? (cep.value = "") : "";
        }
    } catch { }

    const listaCidades = await getCidade(estado);

    if (!cidade.value || !listaCidades.includes(cidade.value)) {
        setCidade(cidade, listaCidades);
    }
}

async function validaCidade(campo) {
    const cidade = campo.value.toLowerCase();

    try {
        const cep = document.querySelector('[name="cep"]');
        if (cep.value) {
            const { municipio } = await getCEP(cep.value);

            if (normalizarTexto(municipio) !== normalizarTexto(cidade)) {
                cep.value = "";
            }
        }
    } catch { }
}

function validaCNPJ(campo) {
    const cnpj = campo.value.replace(/\.|-|\//g, "");

    if (cnpj == "00000000000000") {
        return campo.setCustomValidity("Insira um CNPJ válido");
    }

    function validaDigito(cnpj, digitoVerificador) {
        const posicao = digitoVerificador === 1 ? 12 : 13;
        let multiplicador = digitoVerificador === 1 ? 5 : 6;

        let soma = 0;
        for (let i = 0; i < posicao; i++) {
            if (multiplicador < 2) multiplicador = 9;
            soma += cnpj[i] * multiplicador;
            multiplicador--;
        }

        const resto = soma % 11;
        if (resto < 2) {
            return parseInt(cnpj[posicao]) === 0;
        }

        return parseInt(cnpj[posicao]) === 11 - resto;
    }

    if (!validaDigito(cnpj, 1) || !validaDigito(cnpj, 2)) {
        return campo.setCustomValidity("Insira um CNPJ válido");
    }

    return campo.setCustomValidity("");
}

function validaArquivo(campo) {
    const arquivos = Array.from(campo.files);
    const maxSize = Math.pow(1024, 2) * 8;

    if (!campo.files.item(0)) {
        campo.setCustomValidity("Campo obrigatório.");
        return;
    }

    for (const arquivo of arquivos) {
        if (arquivo.size > maxSize) {
            campo.setCustomValidity("O tamanho do arquivo deve ser menor ou igual a 8MB.");
            return;
        }
    }

    return campo.setCustomValidity("");
}

function normalizarTexto(texto) {
    return texto
        .toLowerCase()
        .replace(/['Â´`]/g, "");
}


