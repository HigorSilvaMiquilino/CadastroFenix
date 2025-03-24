export function mascara(evento) {
    const campo = evento.target;

    const aplicaMascara = {
        cpf: mascaraCPF,
        nomeCompleto: mascaraNome,
        dataNascimento: mascaraData,
        telefone: mascaraTelefone,
        cep: mascaraCEP,
        endereco: mascaraAlfanumerico,
        numero: mascaraAlfanumerico,
        bairro: mascaraAlfanumerico,
        email: mascaraEmail,
        confirmacaoEmail: mascaraEmail,
    };

    if(aplicaMascara[campo.name]) {
        aplicaMascara[campo.name](evento);
    }
}


const mascaraNome = (evento) => {
    evento.target.value = evento.target.value.replace(/[^\p{L}\s]/gu, "").replace(/^\s$/, "");;
    return;
}

const mascaraCPF = (evento) => {
    evento.target.value = evento.target.value.replace(/\D/g, "").replace(/^(\d{3})/g, "$1.").replace(/^(\d{3}\.\d{3})/g, "$1.").replace(/^(\d{3}\.\d{3}\.\d{3})/g, "$1-")
    return;
}

const mascaraData = (evento) => {
    evento.target.value = evento.target.value.replace(/\D/g, "").replace(/^(\d{2})/g, "$1/").replace(/^(\d{2}\/\d{2})/g, "$1/");
    return;
    
}

const mascaraTelefone = (evento) => {
    evento.target.value = evento.target.value.replace(/\D/g, "").replace(/^(\d{1})/g, "($1").replace(/^(\(\d{2})/g, "$1) ").replace(/^(\(\d{2}\)\s\d{4})/g, "$1-").replace(/^(\(\d{2}\)\s\d{4})-(\d{1})(\d{4})/g, "$1$2-$3");
    return;
}

const mascaraCelular = (evento) => {
    evento.target.value = evento.target.value.replace(/\D/g, "").replace(/^(\d{1})/g, "($1").replace(/^(\(\d{2})/g, "$1) ").replace(/^(\(\d{2}\)\s\d{5})/g, "$1-")
    return;
}

const mascaraEmail = (evento) => {
    evento.target.value = evento.target.value.replace(/[^\p{L}0-9@+-_\.]/giu,"").replace(/[\[\]?\/;,\\<>^:]/g, "")
    return;
}

const mascaraCEP = (evento) => {
    evento.target.value = evento.target.value.replace(/\D/g, "").replace(/^(\d{5})/g, "$1-")
    return;
}

const mascaraAlfanumerico = (evento) => {
    evento.target.value = evento.target.value.replace(/[^/\p{L}0-9\/\s-]/giu,"").replace(/[\[\]?;,\\<>^:@+]/g, "");
    return;
}

const mascaraCNPJ = (evento) => {
    evento.target.value = evento.target.value.replace(/\D/g, "").replace(/^(\d{2})/g, "$1.").replace(/^(\d{2}\.\d{3})/g, "$1.").replace(/^(\d{2}\.\d{3}\.\d{3})/g, "$1/").replace(/^(\d{2}\.\d{3}\.\d{3}\/\d{4})/g, "$1-")
    return;
}