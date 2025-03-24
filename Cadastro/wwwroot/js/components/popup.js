export default class Popup extends HTMLElement {
    constructor({titulo = 'Título', descricao, status, botoes = []} = {}) {
        super();

        this.titulo = titulo;
        this.descricao = descricao;
        this.status = status;
        this.botoes = botoes;

        this.render();
    }
    
    render() {
        this.dialog = document.createElement('dialog');
        this.dialog.classList.add('popup');

        this.dialog.appendChild(this.createButton());

        if(this.status) this.dialog.classList.add(this.status);

        const div = document.createElement('div');
        div.classList.add('popup--content');
        div.innerHTML = `
            ${this.status ? `<img class='popup--icone' src='../img/${this.status}.svg' alt=''>` : ''}
            <p class="popup--title">${this.titulo}</p>
            ${this.descricao ? `<p class="popup--text">${this.descricao}</p>` : ''}
        `
        this.dialog.appendChild(div);

        if(this.botoes) {
            const menu = document.createElement('menu');
            menu.classList.add('popup--menu');

            this.botoes.forEach((botao) => {
                const button = document.createElement('button');
                button.classList.add('btn');
                button.classList.add(botao.classe);
                button.textContent = botao.label;

                if (botao.onClick) {
                    button.addEventListener('click', botao.onClick);
                }
                
                menu.appendChild(button);
            })

            this.dialog.appendChild(menu);
        }

        this.closePopupClickedOutside();
        this.appendChild(this.dialog);
        this.appendChild(this.styles());
    }

    styles() {
        const style = document.createElement('style');
        
        style.textContent = `
        .popup::backdrop {
            background-color: rgba(0,0,0,0.75);
            position: fixed;
            top: 0;
            right: 0;
            bottom: 0;
            left: 0;
        }
        .popup {
             display: flex;
             flex-direction: column;
             justify-content: center;
             align-items: center;
             text-align: center;
             opacity: 0;
             visibility: hidden;
             transform: scale(0.5);
             background-color: #191919;
             border-radius: 0.8rem;
             padding: 4rem 2rem;
             outline: none;
             border: none;
             max-width: 300px;
             width: 100%;
             box-sizing: border-box;
             transition: all 0.5s ease;
         }
         .popup[open].in {
             opacity: 1;
             transform: scale(1);
             visibility: visible;
            animation-name: fadeInScaleUp;
            animation-duration: 500ms;
         }
        .popup > .popup__btn-close {
            display: grid;
            place-content: center;
            cursor: pointer;
            width: 3rem;
            height: 3rem;
            color: #fff;
            font-weight: bold;
            font-size: 1.4rem;
            text-align: center;
            border-radius: 0.8rem;
            border: 1px solid rgba(62, 62, 62, 0.6);
            position: absolute;
            top: 5%;
            right: 5%;
            transition: all 500ms ease;
        }

        .popup > .popup__btn-close:hover {
            transform: scale(1.2);
        }

        .popup--icone {
            margin-bottom: 2rem;
            max-width: 130px;
        }

        .popup--title {
            font-size: 3rem;
            text-align: center;
            color: #FFF;
        }

        .popup.sucesso .popup--title {
            color: #51EB0C;
        }

        .popup.erro .popup--title {
            color: #E52B50;
        }

        .popup--text {
            color: rgba(255, 255, 255, 0.7);
            margin-top: 2rem;
        }

        .popup--menu {
            display: flex;
            flex-direction: column;
            justify-content: center;
            gap: 2rem;
            width: 100%;
            margin-top: 2rem;
        }

        .popup--menu button {
            width: 100%;
            transition: transform 500ms ease;
        }

        .popup--menu button:hover {
            transform: scale(1.1);
        }

        @media screen and (min-width: 768px) {
            .popup{
                padding: 5rem 3rem;
                max-width: 520px;
            }
            .popup--menu {
                flex-direction: row;
                width: 100%;
            }
        }
        `;

        return style;
    }

    createButton() {
        const botao = document.createElement('button');
        botao.classList.add('popup__btn-close');
        botao.textContent = 'X';
        botao.title = 'Fechar pop-up'

        botao.addEventListener('click', () => this.closePopup());

        return botao;
    }

    openPopup() {
        this.dialog.classList.add('in');
        this.dialog.showModal();
    }

    closePopup() {
        this.dialog.classList.remove('in');
        this.dialog.close();
    }

    closePopupClickedOutside() {
        this.dialog.addEventListener('click', (event) => {
            const rect = event.target.getBoundingClientRect();
            
            const clickedInDialog = (
                rect.top <= event.clientY &&
                event.clientY <= rect.top + rect.height &&
                rect.left <= event.clientX &&
                event.clientX <= rect.left + rect.width
                );
                
                if(!clickedInDialog) {
                    this.dialog.classList.remove('in');
                    this.dialog.close();
            }
        });
    }

    setTitulo(texto) {
        this.titulo = texto;

        const element = this.dialog.querySelector('.popup--title');
        element.textContent = this.titulo;
    }

    setDescricao(texto) {
        this.descricao = texto;

        const element = this.dialog.querySelector('.popup--text');
        element.textContent = this.descricao;
    }
}

customElements.define('pop-up', Popup);