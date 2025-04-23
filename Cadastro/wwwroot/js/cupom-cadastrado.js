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


async function refreshToken() {
    const response = await fetch("https://localhost:7011/api/v1/auth/refresh-token", {
        method: "POST",
        credentials: "include"
    });

    if (!response.ok) {
        window.location.assign("./login.html");
        throw new Error("Failed to refresh token");
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
            throw error;
        }
    }
    return response;
}

(async () => {
    try {
        const response = await fetchWithAuth("https://localhost:7011/api/v1/Cupom/obterultimocadastrado", {
            method: "GET",
            credentials: "include"
        });

        if (response.ok) {
            const result = await response.json();

            console.log(result);

                const data = result.Metadata.cupom;

                const cnpj = data.CnpjEstabelecimento.replace(/^(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})$/, "$1.$2.$3/$4-$5");

                const dataFormatada = new Date(data.DataCadastro).toLocaleString("pt-BR", {
                    day: "2-digit",
                    month: "2-digit",
                    year: "numeric",
                    hour: "2-digit",
                    minute: "2-digit"
                }).replace(", ", " às ");

                const valorFormatado = new Intl.NumberFormat('pt-BR', {
                    style: 'currency',
                    currency: 'BRL',
                    minimumFractionDigits: 2
                }).format(data.ValorTotal);

                let html = `
                    <tr>
                        <td data-header="CNPJ">${cnpj}</td>
                        <td data-header="Data do cadastro">${dataFormatada}</td>
                        <td data-header="Cupom fiscal">${data.NumeroCupomFiscal}</td>
                        <td data-header="Produtos">${data.quantidadeTotal}</td>
                        <td data-header="Valor">${valorFormatado}</td>
                    </tr>`;

                document.querySelector("table tbody").innerHTML = html;

        } else {
            window.location.assign("./login.html");
        }
    } catch (error) {
       console.error("Erro ao carregar cupom:", error);
       window.location.assign("./login.html");
    }
})();