var zoomImg = function() {
   var clone = this.cloneNode();
   clone.classList.remove("zoom");

   var lb = document.getElementById("lb-img");
   lb.innerHTML = "";
   lb.appendChild(clone);

   lb = document.getElementById("lb-back");
   lb.classList.add("show");
};

window.addEventListener("load", function() {
   var images = document.getElementsByClassName("zoom");
   if (images.length > 0) {
      for (var img of images) {
         img.addEventListener("click", zoomImg);
      }
   }

   document.getElementById("lb-back").addEventListener("click", function() {
      this.classList.remove("show");
   })
});