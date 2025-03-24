export function showHidePassword(input) {
    const botao = input.parentElement.querySelector('.input-pasword--btn__show-hide');
    botao.addEventListener('click', () => {
        const icone = botao.querySelector('i');

        if(input.type === 'password') {
            input.type = 'text';
            icone.classList.replace('bi-eye-fill', 'bi-eye-slash-fill');
            return;
        }

        input.type = 'password';
        icone.classList.replace('bi-eye-slash-fill', 'bi-eye-fill');
    })
}

export function handleRequirementsList(input) {
    const requisitos = [
        {id: 'letrasMaisculas', regex: /[A-Z]/}, //letrasMaisculas
        {id: 'letrasMinusculas', regex: /[a-z]/}, //letrasMinusculas
        {id: 'numeros', regex: /[0-9]{2,}/}, //numeros
        {id: 'caracteresEspeciais', regex: /[^a-zA-Z0-9]/}, //caracteresEspeciais
        {id: 'oitoCaracteres', regex: /.{8,}/} //oitoCaracteres
    ]

    const lista = input.parentElement.querySelector('.input-password--list');

    requisitos.forEach((requisito) => {
        requisito.regex.test(input.value) 
        ? lista.querySelector(`#${[requisito.id]}`).classList.add('valid') 
        : lista.querySelector(`#${[requisito.id]}`).classList.remove('valid') 
    })
    
    for (const erro in input.validity) {
        if ((input.validity[erro] === true || input.value.length <= 1) && erro !== "valid") {
            lista.classList.add('is-active');
            return;
        }
    }

    lista.classList.remove('is-active');
}