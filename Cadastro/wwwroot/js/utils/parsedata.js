export function parseData(match) {
    const dia = parseInt(match[1], 10);
    const mes = parseInt(match[2], 10) - 1;
    const ano = parseInt(match[3], 10);
    const dataFormatada = new Date(Date.UTC(ano, mes, dia));

    return {
        dia,
        mes,
        ano,
        data: dataFormatada,
      };
}

export function ehMaiorDeIdade(data) {
    const dataAtual = new Date();
    const dataMenos18Anos = new Date(dataAtual.getUTCFullYear() - 18, dataAtual.getUTCMonth(), dataAtual.getUTCDate());

    return dataMenos18Anos >= data;
}

export function ehDataValida(dia, mes, ano, data) {
    const ehDataValida = data.getUTCDate() == dia;
    const ehMesValido = data.getUTCMonth() == mes;
    const ehAnoValido = data.getUTCFullYear() == ano && ano >= 1900;

    return ehDataValida && ehMesValido && ehAnoValido;
}

export function ehDataFutura(dia, mes, ano) {
    const dataFormatada = new Date(ano, mes, dia);
    dataFormatada.setHours(0, 0, 0, 0);

    const dataAtual = new Date();
    dataAtual.setHours(0, 0, 0, 0);

    return dataFormatada.getTime() > dataAtual.getTime();
}