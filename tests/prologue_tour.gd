extends SceneTree
## Проход Пролога ботом со снимками экрана (нужен экран, не --headless):
## godot --path . -s res://tests/prologue_tour.gd
var lv
var f := 0
var shots := 0

func _initialize() -> void:
	lv = load("res://scenes/prologue.tscn").instantiate()
	root.add_child(lv)

func _shot(name: String) -> void:
	root.get_viewport().get_texture().get_image().save_png("user://tour_%s.png" % name)
	shots += 1

func _process(_d: float) -> bool:
	f += 1
	var p = lv.player
	# 1) идём по отсекам
	if f < 150:
		Input.action_press("right")
	if f == 150:
		Input.action_release("right")
		p.x = 9.0
	if f == 170: _shot("1_bunker")
	# 2) мастерская: берём вещи, записка
	if f == 180:
		p.x = 18.9
		lv.try_pickup(p)
	if f == 230: _shot("2_note")
	if f == 240:
		lv.note_open = false
		lv.paused = false
		p.x = 24.0
	if f == 260: _shot("3_roots")
	# 3) выстрел вправо в корни
	if f == 262:
		lv.beam_t = 1.0
		lv.beam_dir = Vector2.RIGHT
		lv.heat += 34.0
		lv.alarm += 1
	if f == 280: _shot("4_beam")
	if f == 380: _shot("5_fog")
	if f == 420:
		lv.try_pickup(p)
	if f > 420 and f < 600:
		Input.action_press("right")
	if f == 560: _shot("6_shaft")
	if f == 600:
		Input.action_release("right")
		p.x = 50.0
	if f == 640: _shot("7_city")
	if f == 650:
		p.x = 58.5
	if f == 900:
		_shot("8_title")
		print("shots=%d finished=%s roots_hp=%d fog=%s mask=%s filter=%.0f alarm=%d" % [shots, lv.finished, lv.roots_hp, lv.fog_started, p.mask_on, p.filter_left, lv.alarm])
		return true
	return false
