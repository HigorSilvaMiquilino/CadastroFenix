export async function getCidade(uf) {
    let response = await fetch(`https://servicodados.ibge.gov.br/api/v1/localidades/estados/${uf}/municipios`);
    let resultado = await response.json();
    let cidades = [];
    resultado.forEach(item => {
        cidades.push(item.nome)
    });
    return cidades;
 }

 export function setCidade(select, listaCidades) {
    select.textContent = '';

    const elemento = document.createElement('option');
    elemento.setAttribute('value', '')
    elemento.setAttribute('selected', '')
    elemento.textContent = '*Cidade';
    select.appendChild(elemento);

    listaCidades.forEach(item => {
        const elemento = document.createElement('option');
        elemento.setAttribute('value', item)
        elemento.textContent = item;
        select.appendChild(elemento);
    })
}