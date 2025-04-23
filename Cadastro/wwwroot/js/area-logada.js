

const btnLogout = document.querySelector("#btnLogout");
const usuarioNome = document.querySelector("#usuarioNome");

function displayUsuario() {
    const nome = localStorage.getItem("nome");
    if (nome) {
        usuarioNome.textContent = `Oi, ${nome}`;
    } else {
        usuarioNome.textContent = "Olá, Usuário";
        console.warn("Nome não encontrado no localStorage. Usando valor padrão.");
    }
}

async function logOut() {
    try {
        const response = await fetch("https://localhost:7011/api/v1/Auth/logout", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                credentials: "include",
            },
        });
        if (response.ok) {
            localStorage.removeItem("nome");
            localStorage.setItem("login", JSON.stringify({ conectado: false }));
            window.location.assign("./login.html");
        } else {
            throw new Error("Erro ao fazer logout.");         
        }
    } catch (error) {
        console.error("Erro ao fazer logout:", error);
    }
}

document.addEventListener("DOMContentLoaded", () => {
    displayUsuario();
});

btnLogout.addEventListener("click", logOut);