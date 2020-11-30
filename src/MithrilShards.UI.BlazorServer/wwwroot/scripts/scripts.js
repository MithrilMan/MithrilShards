'use strict';

window.openedWindows = {};

function openSymbolUrl(symbolName) {
   var url = `https://www.binance.com/en/futures/${symbolName}`;

   var symbolUrl = null;

   if (symbolName in window.openedWindows && window.openedWindows[symbolName].closed == false) {
      symbolUrl = window.openedWindows[symbolName];
   }
   else {
      symbolUrl = window.open(url, `window_${symbolName}`, null, false);
      window.openedWindows[symbolName] = symbolUrl;
   }

   symbolUrl.focus();
}


function copyElementToClipboard(sourceElement) {
   var range = document.createRange();
   range.selectNode(sourceElement);
   window.getSelection().removeAllRanges(); // clear current selection
   window.getSelection().addRange(range); // to select text
   document.execCommand("copy");
   window.getSelection().removeAllRanges();// to deselect
}