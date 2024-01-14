// import { add_star } from "./star.js";

// var htmlOutput;
// // récupération de la balise result
// htmlOutput = document.getElementById("result");

// var myTinCan; 
// try{ 
//     myTinCan = new TinCan();    
//     myTinCan.lrs = new TinCan.LRS( 
//     { 
//         endpoint: "https://lrsels.lip6.fr/data/xAPI", 
//         username: "9fe9fa9a494f2b34b3cf355dcf20219d7be35b14", 
//         password: "b547a66817be9c2dbad2a5f583e704397c9db809" 
//     }); 
//     htmlOutput.innerHTML = "Connection avec le LRS OK<br>"; 
//     // Construction d'une requête pour récupérer tous les statements d'un agent 
//     htmlOutput.innerHTML += "Envoi requête, en attente de réponse...<br>"; 
//     myTinCan.lrs.queryStatements( 
//     { 
//         params: {    
//         agent: new TinCan.Agent({ 
//                     account: { 
//                         "homePage": "https://www.lip6.fr/mocah/", 
//                         "name": "MATHIEU" } 
//                 }), 
//         limit: 100 
//         }, 
//         callback: function (err, response) { 
//         if (err !== null) { 
//             console.log("Failed to query statements: " + err); 
//             return; 
//         } 
//         htmlOutput.innerHTML += "Nombre de statements reçus : " + response.statements.length + 
//     "<br>"; 
//         } 
//     });
// } 
// catch (ex) { 
//     console.log("Failed to setup LRS object: "+ ex);
//     htmlOutput.innerHTML = "Failed to setup LRS object: "+ ex; 
// }

var listClass = [];
let myTinCan = setup_TinCan();
const jsConfetti = new JSConfetti();
let list_goal = [];
let last_statement_per_student = {};
let student_level_star = {};
let student_progress_nb_star = {};

// la fonction pour ajouter un élève dans la liste de la classe 
function addStudent() {
    // on récupère le nom de l'élève dans le formulaire
    var student = document.getElementById("input_student_id").value;
    // on vérifie si l'élève n'est pas déjà dans la liste
    var in_list = false;
    for (var i = 0; i < listClass.length; i++) {
        if (student == listClass[i]) {
            in_list = true;
        }
    }
    if (in_list) {
        alert(student + " est déjà dans la classe");
        return;
    }
    // on vérifie si l'élève existe déjà dans le LRS
    check_if_student_exist(student).then(exist => {
        if (exist) {
            console.log("ajout de l'élève " + student);
            // on ajoute l'élève dans la liste de la classe
            listClass.push(student);
            // on met à jour l'affichage de la liste
            displayList();
            // on active le bouton pour exporter la liste si le bouton n'est pas déjà actif
            if (document.getElementById("start").disabled) {
                document.getElementById("start").disabled = false;
            }
        }
        else {
            alert(student + " n'existe pas dans le LRS");
        }
    }).catch(err => {
        console.log(err); // erreur lors de la requête
        alert("Erreur lors de la requête");
    });
    // on vide le formulaire
    document.getElementById("input_student_id").value = "";
}

// la fonction pour afficher la liste de la classe
function displayList() {
    // on récupère la balise html qui va contenir la liste
    var list = document.getElementById("student_list");
    // on vide la balise
    list.innerHTML = "";
    // on parcourt la liste de la classe
    for (var i = 0; i < listClass.length; i++) {
        // on ajoute un élément de la liste
        list.innerHTML += "<li>" + listClass[i] + "</li>";
    }
}

// la fonction pour vider la liste de la classe
function resetClass() {
    console.log("reset de la classe");
    // on vide la liste de la classe
    listClass = [];
    // on met à jour l'affichage de la liste
    displayList();
    // on désactive le bouton pour exporter la liste
    document.getElementById("start").disabled = true;
}

function setup_TinCan() {
    var myTinCan; 
    var status_div = document.getElementById("connection_status");
    try {
        myTinCan = new TinCan();
        myTinCan.lrs = new TinCan.LRS({
            endpoint: "https://lrsels.lip6.fr/data/xAPI",
            // username: "9fe9fa9a494f2b34b3cf355dcf20219d7be35b14", // PROD
            username: "e6efcf3eac5c03e121af621dae0df3a50c8733f0", // DEV
            // password: "b547a66817be9c2dbad2a5f583e704397c9db809", // PROD
            password: "ffda037ebf1368a89e5b8b59d30a1b77beebc27e", // DEV
        });
        status_div.innerHTML = "✅Connection avec le LRS OK<br>";
        // on change la couleur du status en vert 
        status_div.style.backgroundColor = "#1ed760";
    } catch (ex) {
        console.log("❗Failed to setup LRS object: " + ex);
        status_div.innerHTML = "❗Failed to setup LRS object: " + ex;
        // on change la couleur du status en rouge
        status_div.style.backgroundColor = "red";
    }
    return myTinCan;
}

function check_if_student_exist(student_id) {
    // on envoie une requête pour vérifier si l'élève existe déjà dans le LRS
    return new Promise((resolve, reject) => {
        myTinCan.lrs.queryStatements({
            params: {
                agent: new TinCan.Agent({
                    account: {
                        "homePage": "https://www.lip6.fr/mocah/",
                        "name": student_id
                    }
                }),
                limit: 1
            },
            callback: function (err, response) {
                if (err !== null) {
                    console.log("Failed to query statements: " + err);
                    reject(err);
                }
                if (response.statements.length > 0) {
                    console.log(student_id + " existe dans le LRS");
                    resolve(true);
                }
                else {
                    console.log(student_id + " n'existe pas dans le LRS");
                    resolve(false);
                }
            }
        });
    });
}

// la fonction pour voir les statements d'un élève
function start_viz(){
    // on attend quelque secondes pour que le LRS soit prêt
    console.log(listClass);
    display_goal();
    setup_stats_selector(listClass);
    display_stats();
    // on recupère le main-canvas
    var main_canvas = document.getElementById("main-canvas");
    // on display le main-canvas si il n'est pas déjà affiché
    if (main_canvas.style.display != "flex"){
        main_canvas.style.display = "flex";
    }
    else{
        return;
    }
    setInterval(loop_scan, 30000);    
}

function add_goal(){
    var goal = document.getElementById("input_goal").value;
    // onvérifie si l'objectif est un nombre
    if (isNaN(goal)) {
        alert("L'objectif doit être un nombre");
        document.getElementById("input_goal").value = "";
        return;
    }
    // on vérifie si l'objectif n'est pas déjà dans la liste
    for (var i = 0; i < list_goal.length; i++) {
        if (goal == list_goal[i]) {
            alert("L'objectif est déjà dans la liste");
            document.getElementById("input_goal").value = "";
            return;
        }
    }
    // on ajoute l'objectif dans la liste
    list_goal.push(Number(goal));
    // on trie la liste des objectifs
    list_goal.sort((a, b) => a - b);
    // on vide le formulaire
    document.getElementById("input_goal").value = "";
    console.log('nouvelle objectif : ' + goal);
    console.log(list_goal);
    display_goal();
}

function display_goal(){
    var goal_display_element = document.getElementById("goal_list");
    goal_display_element.innerHTML = "";
    for (var i = 0; i < 1; i++) {
        if (list_goal[i] == null) {
            break;
        }
        percentage_completed = Math.round(get_number_of_stars() / list_goal[i] * 100);
        goal_display_element.innerHTML += "<div id='current-goal'><img src='target.png' class='target-icon'>Objectif Actuel : " + list_goal[i] + " étoiles ("+ percentage_completed +"%) </div>";
    }
}

function get_current_goal(){
    return list_goal[0];
}

function remove_current_goal(){
    list_goal.shift();
    display_goal();
}

function add_new_star(){

    let latest_goal = get_current_goal();
    // console.log(latest_goal);
    if (latest_goal != null) {
        if (latest_goal <= get_number_of_stars()) {
            // we add the achive goal to the list of goal completed
            add_achieved_goal();
            // we remove the goal
            remove_current_goal();
            // we play confetti
            setTimeout(confettis, 0);
            setTimeout(confettis, 1000);
            setTimeout(confettis, 2000);
            setTimeout(confettis, 3000);

        }
    }

    add_star();
    display_goal();
}

function add_achieved_goal(){
    // we add the achive goal to the list of goal completed
    var goal_achieved = document.getElementById("goal_achieved");
   
    var medal_icon = document.createElement("img");
    medal_icon.setAttribute("src", "medal.png");
    medal_icon.setAttribute("class", "medal-icon");

    var goal_achieved_text = document.createElement("div");
    goal_achieved_text.setAttribute("class", "goal-achieved-text");
    goal_achieved_text.innerHTML = get_current_goal() + " étoiles ";
    
    var goal_package = document.createElement("div");
    goal_package.setAttribute("class", "goal-package");
    goal_package.appendChild(medal_icon);
    goal_package.appendChild(goal_achieved_text);
    goal_achieved.appendChild(goal_package);
}

// la fonction pour récupérer les statements d'un élève
function get_statements(student_id) {
    // on envoie une requête pour récupérer les statements de l'élève
    return new Promise((resolve, reject) => {
        myTinCan.lrs.queryStatements({
            params: {
                agent: new TinCan.Agent({
                    account: {
                        "homePage": "https://www.lip6.fr/mocah/",
                        "name": student_id
                    }
                }),
                limit: 30,
                verb: new TinCan.Verb({ 
                    id: "http://adlnet.gov/expapi/verbs/completed", 
              }),
            },
            callback: function (err, response) {
                if (err !== null) {
                    console.log("Failed to query statements: " + err);
                    reject(err);
                }
                resolve(response.statements);
            }
        });
    });
}

function loop_scan(){
    var j = 0;
    // on parcours la liste de la classe en récupérant les statements de chaque élève
    for (var i = 0; i < listClass.length; i++) {
        // on récupère les statements de l'élève
        get_statements(listClass[i]).then(statements => {
            // on affiche les statements
            var name = listClass[j];
            //si on récupère des statements, on vas recuperer le nom
            if (statements.length > 0){
                name = statements[0]["actor"]["name"];
            }
            j++;
            // display_result(statements, name);
            // on récupère les nouveaux statements completed
            let new_statements = get_new_statement(statements, name);
            // console.log("nouveaux statements : ");
            // console.log(new_statements);
            let nb_star_to_add = 0;
            // on parcourt les nouveaux statements
            for (var k = 0; k < new_statements.length; k++) {
                star_element = new_statements[k]["result"]["extensions"]["https://spy.lip6.fr/xapi/extensions/value"][0];
                level_name = new_statements[k]["result"]["extensions"]["https://w3id.org/xapi/seriousgames/extensions/progress"][0];
                // on vérifie si star_element est un nombre
                if (isNaN(star_element)) {
                    console.log("star_element n'est pas un nombre");
                    continue;
                }
                // les véritables étoiles que l'eleve a gagné
                let true_new_star = 0;
                if (student_level_star[name] == null){
                    student_level_star[name] = {};
                }
                if (student_level_star[name][level_name] == null){
                    student_level_star[name][level_name] = Number(star_element);
                    true_new_star = Number(star_element);
                }
                else{
                    student_level_star[name][level_name] = Number(star_element)>student_level_star[name][level_name] ? Number(star_element) : student_level_star[name][level_name] ;
                    true_new_star = Number(star_element) - student_level_star[name][level_name];
                }
                // on ajoute le nombre d'étoile à l'élève
                if (student_progress_nb_star[name] == null){
                    student_progress_nb_star[name] = [];
                }
                student_progress_nb_star[name].push(true_new_star + student_progress_nb_star[name][student_progress_nb_star[name].length - 1]);
                    
                nb_star_to_add += true_new_star;
                // on ajoute le nombre d'étoile à l'élève
                student_level_star
            }
            
            if (student_progress_nb_star[name] == null){
                student_progress_nb_star[name] = [];
            }

            if (nb_star_to_add == 0){
                student_progress_nb_star[name].push(0 + student_progress_nb_star[name][student_progress_nb_star[name].length - 1]);
            }

            //console.log(statements[0]["result"]["extensions"]["https://spy.lip6.fr/xapi/extensions/value"][0]);
            // on ajoute une étoile pour chaque statement avec un délai de 1 seconde
            for (var k = 0; k < nb_star_to_add; k++) {
                noise = Math.random() * 100;
                setTimeout(add_new_star, 300 * k + noise);
            }

        }).catch(err => {
            console.log(err); // erreur lors de la requête
        });
    }
}

function get_student_level_star(selected_student){
    return student_level_star[selected_student];
}

function get_progress_nb_star(selected_student){
    return student_progress_nb_star[selected_student];
}

function get_new_statement(statements, name){
    // console.log("name : " + name);
    // console.log("last statement : ");
    // console.log(last_statement_per_student[name]);
    // console.log("new statements : ");
    // console.log(statements);
    
    let new_statements = [];
    // on vérifie si la liste des derniers statements est vide
    if (last_statement_per_student[name] == null){
        // on ajoute le dernier statement dans la liste
        last_statement_per_student[name] = statements[0];
        return new_statements;
    }
    // on cherche le dernier statement dans la liste des statements pour 
    // trouver les nouveaux statements
    for (var i = 0; i < statements.length; i++) {
        // on vérifie si le statement est déjà dans la liste
        if (statements[i]["id"] == last_statement_per_student[name]["id"]){
            //on ajoute les nouveaux statements dans la liste
            for (var j = 0; j < i; j++) {
                new_statements.push(statements[j]);
            }
            // on met à jour le dernier statement
            last_statement_per_student[name] = statements[0];
            return new_statements;
        }
    }
    // si on arrive ici, c'est que le dernier statement n'est pas dans la liste
    // on ajoute tous les statements dans la liste
    for (var i = 0; i < statements.length; i++) {
        new_statements.push(statements[i]);
    }
    // on met à jour le dernier statement
    last_statement_per_student[name] = statements[0];
    return new_statements;

}

// la fonction pour afficher les statements
function display_result(statements, name) {
    // on créé une balise pour afficher les statements
    var result = document.createElement("div");
    result.setAttribute("class", "result");
    result.innerHTML = "<h2>Statements</h2>";
    // on affiche le nombre de statements
    result.innerHTML += "<p>" + statements.length + " level(s) completed for " + name + "</p>";
    // on ajoute la balise dans l'html pour l'afficher
    window.document.body.appendChild(result);
    
}

function get_max_star_number(list_student){
    number_level = 41
    return number_level * list_student.length * 3;
}

function confettis(){
    jsConfetti.addConfetti({
        emojis: ['🌟'],
        emojiSize: 50,
        confettiNumber: 20,
    });
     jsConfetti.addConfetti({
        confettiColors: [
          '#ff0a54', '#ff477e', '#ff7096', '#ff85a1', '#fbb1bd', '#f9bec7',
        ],
    });

}

function manual_add_star(nb_star){
    for (var i = 0; i < nb_star; i++) {
        setTimeout(add_new_star, 300 * i);
    }
}

function display_stats(){
    stat_element = document.getElementById("statistic");
    // on change la valeur de display
    if (stat_element.style.display != "flex"){
        stat_element.style.display = "flex";
    }
}

function setup_stats_selector(list_options){
    var stats_selector = document.getElementById("stats_selector");
    for (var i = 0; i < list_options.length; i++) {
        let option = document.createElement("option");
        option.setAttribute("value", list_options[i]);
        option.innerHTML = list_options[i];
        stats_selector.appendChild(option);
    }
}

// document.getElementById("input_student_id").value = "MATHIEU";
// addStudent();
// document.getElementById("input_student_id").value = "8D9D4B65";
// addStudent();
// document.getElementById("input_student_id").value = "5CF68E0F";
// addStudent();
// document.getElementById("input_student_id").value = "C46DA0F6";
// addStudent();

// setTimeout(start_viz, 3000);
