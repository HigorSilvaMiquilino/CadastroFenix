export async function getCEP(cep) {
    let response = await fetch(`https://viacep.com.br/ws/${cep}/json/`);
    let localizacao = await response.json();

    return {
         erro: localizacao.erro,
         uf: localizacao.uf,
         municipio: localizacao.localidade,
         bairro: localizacao.bairro,
         logradouro: localizacao.logradouro
     };
 }

//  73990000
//  76650000