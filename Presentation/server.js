const express = require('express');
const path = require('path');
const app = express();
const port = 3000;

// Servir les fichiers statiques du dossier courant
app.use('/presentation', express.static(__dirname));

// Route principale vers l'index.html
app.get('/presentation', (req, res) => {
    res.sendFile(path.join(__dirname, 'index.html'));
});

app.listen(port, '0.0.0.0', () => {
    console.log(`Présentation TowerFluffy active sur http://0.0.0.0:${port}/presentation`);
});
