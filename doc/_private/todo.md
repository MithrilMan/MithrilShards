Le consensus rule le definirò con delle extensions/metodi parlanti

Un modo potrebbe essere quello di avere una classe che espone tutti dati base necessari alle regole affinchè vengano eseguite.

Ad esempio ci deve essere un modo per recuperare il previous output di una transazione.

Il codice di tale controllo (guard?) si potrebbe aggiungere ad una specie di dictionary, di modo che altre regole possano sfruttare lo stesso codice, sfruttando magari il concetto di cache (se un altra regola ha già chiamato quella funzione, il valore restituito deve essere lo stesso e quindi si può restituire il valore precedente, che va quindi cachato)

Un modello di riferimento a quanto ho in mente è il funzionamento di ngrx per angular: 
Bisogna avere uno stato immutabile che può essere restituito a chi necessità di informazioni.

L'immutabilità dello stato fa si che non ci possano essere errori dove per sbaglio una regola cambia il valore dello stato e quindi un'altra regola che richiede la stessa informazione otterrà valori diversi (le regole devono essere idempotenti)

Continuando l'analogia con ngrx, lo stato esportato alle varie rules deve essere ovviamente uno stato comune e le regole potrebbero agire a livello di selector.

Il valore iniziale dello stato va impostato all'inizio del ciclo di validazione.
L'unico momento in cui lo stato si può modificare è quando una transazione viene validata (full) in quanto in quel momento sta virtualmente alterando lo stato nuovo (e.g. dobbiamo evitare il double spending quindi dobbiamo evitare di mettere nello stesso blocco due transazioni che spendono lo stesso input)

In alternativa potremmo avere uno stato parallelo contenente solo le transazioni validate, questo però richiederebbe al consensus rule di agire su due stati diversi (ma non sarebbe un problema e forse sarebbe pure una soluzione più appropriata)