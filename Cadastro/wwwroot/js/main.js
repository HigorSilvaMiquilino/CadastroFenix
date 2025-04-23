document.addEventListener("DOMContentLoaded", function () {
    const svgBox = document.querySelector(".svgBox");
    const svgLogo = document.querySelector(".svgLogo");
    const formLogin = document.querySelector(".section__login-form");
  
    svgBox.classList.add("animate__fadeIn");
  
    setTimeout(() => {
      svgLogo.classList.replace("hidden", "animate__fadeInDown");
      formLogin.classList.replace("hidden", "animate__fadeInUp");
    }, 1000);
  });