var canvas = document.getElementById("main-canvas");
var add_star_button = document.getElementById("add-star");
var remove_all_stars_button = document.getElementById("remove-all-stars");
width = 800;
height = 800;
total_nb_stars = 0;
star_size = 20;

star_spawn_position = {
    x: 400,
    y: 50
};

var Engine = Matter.Engine,
        Render = Matter.Render,
        Runner = Matter.Runner,
        Composite = Matter.Composite,
        Composites = Matter.Composites,
        Common = Matter.Common,
        MouseConstraint = Matter.MouseConstraint,
        Mouse = Matter.Mouse,
        Bodies = Matter.Bodies;

// create engine
var engine = Engine.create(),
    world = engine.world;

// create renderer
var render = Render.create({
    element: canvas,
    engine: engine,
    options: {
        width: width,
        height: height,
        showAngleIndicator: true,
        wireframes: false,
        background: 'transparent'
    }
});

Render.run(render);

// create runner
var runner = Runner.create();
Runner.run(runner, engine);

// add border to the cups
Composite.add(world, [
    Bodies.rectangle(200, height/2, 50.5, 600, { isStatic: true, render: { fillStyle: '#060a19', visible : false} })
]);

Composite.add(world, [
    Bodies.rectangle(600, height/2, 50.5, 600, { isStatic: true, render: { fillStyle: '#060a19', visible: false } })
]);

Composite.add(world, [
    Bodies.rectangle(400, 700, 400, 50.5, { isStatic: true, render: { fillStyle: '#060a19' , visible: false} })
]);

// add a star list
var star_list = [];

// add the jar image to the canvas
var jar = Bodies.rectangle(600, height/2, 0.1, 600, { isStatic: true, render: { sprite: { texture: 'jar.png' , xOffset:0.36 } } });

Composite.add(world, jar);

function add_big_star() {
    size_factor = 3;
    var big_star = Bodies.polygon(star_spawn_position.x, star_spawn_position.y, 5, star_size*size_factor, {restitution: 0.8, friction: 0.1,  render:{
        sprite:{
            texture: "100_star.png",
            xScale: 0.15*size_factor,
            yScale: 0.15*size_factor
        },
        strokeStyle: '#ffffff',
        fillStyle: '#ffffff',
    }});
    Matter.Body.setMass(big_star, 0.1);

    console.log(big_star);
    // star_list.push(full_jar);
    Composite.add(world, big_star);
    big_star.plugin.wrap = {
        min: { x: 0, y: 0 },
        max: { x: width, y: height }
    };

    // we remove jar from the world so the jar is always on top of the stars
    Composite.remove(world, jar);
    // we add the jar back to the world
    Composite.add(world, jar);
}

function add_star() {
    // if the number of stars is greater than 100, we remove all the stars
    // and update the star counter
    if (star_list.length == 100) {
        remove_all_stars();
        add_big_star();
    }


    var star = Bodies.polygon(star_spawn_position.x, star_spawn_position.y, 5, star_size, {restitution: 0.8, friction: 0.01,  render:{
        sprite:{
            texture: "star.png",
            xScale: 0.15,
            yScale: 0.15
        },
        strokeStyle: '#ffffff',
        fillStyle: '#ffffff',
    }});
    Matter.Body.setMass(star, 0.1);

    // console.log(star);
    star_list.push(star);
    Composite.add(world, star);
    star.plugin.wrap = {
        min: { x: 0, y: 0 },
        max: { x: width, y: height }
    };
    // we remove jar from the world so the jar is always on top of the stars
    Composite.remove(world, jar);
    // we add the jar back to the world
    Composite.add(world, jar);
    total_nb_stars++;

    update_star_count();
}

function remove_all_stars() {
    for (var i = 0; i < star_list.length; i++) {
        Composite.remove(world, star_list[i]);
    }
    star_list = [];
}

function get_number_of_stars() {
    return total_nb_stars;
}

function update_star_count() {
    // si le compteur existe sur la page, on le met à jour
    // sinon on affiche juste dans le terminal
    if (document.getElementById("star-counter")) {
        document.getElementById("star-counter").innerHTML = total_nb_stars;
    }
    else {
        console.log(total_nb_stars);
    }
}


// link the button to the function
// add_star_button.onclick = add_star;
// remove_all_stars_button.onclick = remove_all_stars;


// setInterval(add_star, 100);


