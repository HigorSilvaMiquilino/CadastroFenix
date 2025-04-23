export default class ProdutoForm extends HTMLElement {
  constructor() {
    super();

    this.innerHTML = `
            <form id="produtos">
                <fieldset class="is-active animate__fadeIn">
                    <div class="input__container">
                        <label for="produto" class="label">Produto</label>
                        <div class="wrapper">
                            <i class="bi bi-basket"></i>
                            <select name="produto" id="produto" class="select" required>
                                <option value="" selected>Selecione o produto</option>
                            </select>
                        </div>
                        <span class="input__error-msg"></span>
                    </div>
                    <div class="input__container">
                        <label for="quantidade" class="label">Quantidade</label>
                        <div class="wrapper">
                            <i class="bi bi-123"></i>
                            <input autocomplete="off" type="text" class="input" id="quantidade" inputmode="numeric" name="quantidade" placeholder="Quantidade" maxlength="6" required />
                        </div>
                        <span class="input__error-msg"></span>
                    </div>
                    <div class="input__container">
                        <label for="valor" class="label">Valor unitário (R$)</label>
                        <div class="wrapper">
                            <i class="bi bi-coin"></i>
                            <input autocomplete="off" type="text" class="input" id="valor" inputmode="decimal" name="valor" placeholder="Valor unitário (R$)" maxlength="6" required />
                        </div>
                        <span class="input__error-msg"></span>
                    </div>
                </fieldset>
                <button type="submit" class="btn btn--gray">Adicionar produto</button>
                <span class="input__error-msg" style="text-align: center;" id="erroQuantidadeTotal"></span>
            </form>
            <div class="produtos--container">
                <table class="table">
                    <thead class="table--header">
                        <tr>
                            <th>Produto</th>
                            <th>Quantidade</th>
                            <th>Valor unitário</th>
                            <th>
                                <svg xmlns="http://www.w3.org/2000/svg" width="23" height="26" viewBox="0 0 23 26" fill="none">
                                    <path id="Vector" d="M1 6.33333H22M8.875 11.6667V19.6667M14.125 11.6667V19.6667M2.3125 6.33333L3.625 22.3333C3.625 23.0406 3.90156 23.7189 4.39384 24.219C4.88613 24.719 5.55381 25 6.25 25H16.75C17.4462 25 18.1139 24.719 18.6062 24.219C19.0984 23.7189 19.375 23.0406 19.375 22.3333L20.6875 6.33333M7.5625 6.33333V2.33333C7.5625 1.97971 7.70078 1.64057 7.94692 1.39052C8.19306 1.14048 8.5269 1 8.875 1H14.125C14.4731 1 14.8069 1.14048 15.0531 1.39052C15.2992 1.64057 15.4375 1.97971 15.4375 2.33333V6.33333" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                                </svg>
                            </th>
                        </tr>
                    </thead>
                    <tbody class="table--content">
                    <tr class="table--content__default">
                        <td colspan="4">Nenhum produto adicionado</td>
                    </tr>
                    </tbody>
                </table>
            </div>
            <p class="text--total">Total R$ <span data-total>0,00</span></p>
        `;

    document.addEventListener("DOMContentLoaded", () => {
        fetch("https://localhost:7011/api/v1/Produto/Produtos")
        .then((response) => response.json())
        .then((produtos) => {
          const select = document.getElementById("produto");
          produtos.forEach((produto) => {
            const option = document.createElement("option");
              option.value = produto.descricao;
              option.textContent = produto.descricao;
            option.setAttribute("data-id", produto.Id);
            select.appendChild(option);
          });
        })
        .catch((error) => console.error("Erro ao carregar produtos:", error));
    });

    this.form = this.querySelector("#produtos");
    this.tableContent = this.querySelector(".table--content");
    this.totalElement = this.querySelector("[data-total]");
    this.qtdTotal = 0;
  }

  connectedCallback() {
    this.form.addEventListener("submit", this.onFormSubmit.bind(this));
  }

  disconnectedCallback() {
    this.form.removeEventListener("submit", this.onFormSubmit);
  }

  onFormSubmit(event) {
    event.preventDefault();

    const id = this.form.querySelector("#produto option:checked").getAttribute("data-id");
    const produto = this.form.querySelector("#produto").value;
    const quantidade = parseFloat(this.form.querySelector("#quantidade").value.replace(",", "."));
    const valor = parseFloat(this.form.querySelector("#valor").value.replace(",", "."));

    const valorProduto = quantidade * valor;

    this.atualizaValorTotal(valorProduto);
    this.atualizaQuantidadeTotal(quantidade);

    this.adicionaProduto({ id, produto, quantidade, valor });

    const campos = this.form.querySelectorAll("[required]");
    campos.forEach((campo) => campo.classList.remove("is-valid"));

    this.form.reset();
  }

  adicionaProduto(produto) {
    const row = document.createElement("tr");
    const valor = produto.valor.toLocaleString("pt-BR", { minimumFractionDigits: 2 });

    row.setAttribute("data-id", produto.id);
    row.setAttribute("data-descricao", produto.produto);
    row.setAttribute("data-qtd", produto.quantidade);
    row.setAttribute("data-valor", valor);

    row.innerHTML = `
            <td data-header="Produto">${produto.produto}</td>
            <td data-header="Quantidade">${produto.quantidade}</td>
            <td data-header="Valor">${valor}</td>
            <td data-header="Excluir">
                <button style="width: 3.5rem" class="btn-remove btn--red" title="Remover produto">X</button>
            </td>
        `;

    const removeButton = row.querySelector(".btn-remove");
    removeButton.addEventListener("click", () => {
      this.removeProduto(row, produto.quantidade, produto.valor);
    });

    this.tableContent.appendChild(row);

    this.dispatchEvent(
      new CustomEvent("atualizacao-produtos", {
        detail: {
          quantidadeTotal: this.getQuantidadeTotal(),
          valorTotal: this.getValorTotal(),
        },
        bubbles: true,
        composed: true,
      })
    );
  }

  removeProduto(row, quantidade, valor) {
    row.remove();

    const quantidadeRemovida = parseInt(quantidade);
    const valorRemovido = parseFloat(valor);

    const valorTotal = parseFloat(quantidadeRemovida * valorRemovido);

    this.atualizaValorTotal(-valorTotal);
    this.atualizaQuantidadeTotal(-quantidadeRemovida);

    this.dispatchEvent(
      new CustomEvent("atualizacao-produtos", {
        detail: {
          quantidadeTotal: this.getQuantidadeTotal(),
          valorTotal: this.getValorTotal(),
        },
        bubbles: true,
        composed: true,
      })
    );
  }

  atualizaValorTotal(valor) {
    const totalAtual = this.getValorTotal();
    const novoTotal = totalAtual + valor;

    this.totalElement.textContent = novoTotal.toLocaleString("pt-BR", { minimumFractionDigits: 2 });
  }

  getValorTotal() {
    return parseFloat(this.totalElement.textContent.replace(/\./g, "").replace(",", "."));
  }

  atualizaQuantidadeTotal(quantidade) {
    const totalAtual = this.getQuantidadeTotal();
    this.qtdTotal = totalAtual + quantidade;
  }

  getQuantidadeTotal() {
    return this.qtdTotal;
  }
}

customElements.define("produto-form", ProdutoForm);
