extends SceneTree
## Автопроверка без окна: godot --headless --path . -s res://tests/scenario_test.gd
## Бот проходит уровень тремя способами и печатает итог.

var main
var scenario := 0
var frames := 0
var results := []
var phase := ""

func _initialize() -> void:
	_start()

func _start() -> void:
	if main:
		main.queue_free()
	for a in ["left", "right", "run", "sneak", "freeze"]:
		if InputMap.has_action(a):
			Input.action_release(a)
	main = load("res://scenes/main.tscn").instantiate()
	root.add_child(main)
	frames = 0
	phase = "go"

var heard_log := {}
var holding_bot := false
func _press(a: String, on: bool) -> void:
	if on:
		Input.action_press(a)
	else:
		Input.action_release(a)

func _process(delta: float) -> bool:
	if main == null or not main.is_inside_tree():
		return false
	frames += 1
	var p = main.player
	var near: float = main.nearest_symbiote_distance(p.x)
	match scenario:
		0: # просто идёт шагом вправо
			_press("right", true)
		1: # бежит
			_press("right", true); _press("run", true)
		2: # крадётся; если симбиот рядом — стоит, а вплотную задерживает дыхание
			_press("sneak", true)
			var close := near < 3.0
			if holding_bot:
				if near > 1.3 or p.hold_left < p.hold_capacity() * 0.3:
					holding_bot = false
			elif near < 1.3 and p.hold_left >= p.hold_capacity() * 0.9:
				holding_bot = true
			_press("freeze", holding_bot)
			_press("right", not close and not holding_bot)
	if main.finished or frames > 60 * 240:
		results.append("сценарий %d: дошёл=%s время=%.0f с фильтр=%.0f услышали=%d контактов=%d x=%.1f" % [scenario, main.finished, main.now, p.filter_left, main.noticed, main.contacts, p.x] + "  " + str(main.heard_stats))
		scenario += 1
		if scenario > 2:
			for r in results:
				print(r)
			return true
		_start()
	return false
