let fake_data = []

let chart = Highcharts.chart('star-chart', {

    title: {
        text: 'La courbe d\'évolution des étoiles pour chaque apprenant',
        align: 'left'
    },

    yAxis: {
        title: {
            text: "Nombre d'étoiles"
        }
    },

    xAxis: {
        title: {
            text: "Temps ( nombre d'intervales de 30 secondes )"
        }
    },

    legend: {
        layout: 'vertical',
        align: 'right',
        verticalAlign: 'middle'
    },

    plotOptions: {
        series: {
            label: {
                connectorAllowed: false
            },
            pointStart: 0
        }
    },

    series: [{
        name: 'Apprenant',
    },],

    responsive: {
        rules: [{
            condition: {
                maxWidth: 500
            },
            chartOptions: {
                legend: {
                    layout: 'horizontal',
                    align: 'center',
                    verticalAlign: 'bottom',
                },
                backgroundColor: '#000'
            }
        }]
    },

    credits: {
        enabled: false
    },
    chart: {
        backgroundColor: 'transparent'
    },
});

function update_plot(){
    // on récupère le nom de l'élément dans le selecteur
    let selected = document.getElementById('stats_selector').value
    console.log("selection des données de " + selected)
    // on récupère les données correspondantes
    let data = get_data(selected)
    console.log(data)
    // on met à jour le graphique
    chart.series[0].setData(data)
    chart.series[0].update({
        name: selected
    })
}

function get_data(selected){
    // let nb_star = 0;
    // let results = get_student_level_star(selected);
    // let levels_name = Object.keys(results);
    // for (let i = 0; i < levels_name.length; i++) {
    //     nb_star += results[levels_name[i]];
    // }
    // return nb_star;
    return get_progress_nb_star(selected);
}

// console.log(chart)

// let series = chart.series[0]

// setInterval(function() {
//     fake_data.push(Math.random() * 1000)
//     series.addPoint(fake_data[fake_data.length - 1])
//     console.log(fake_data)
// }, 1000)

