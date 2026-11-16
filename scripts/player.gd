class_name Player
extends Node2D
## Артём: движение, шум шагов и дыхания, задержка дыхания, фильтр.

var main: Node            # ссылка на главный скрипт уровня
var x := 0.0              # позиция по горизонтали, в метрах
var facing := 1
var mode := "still"       # still / sneak / walk / run / freeze / change

var step_timer := 0.0
var breath_timer := 0.0
var run_time := 0.0
var winded := 0.0         # сколько ещё длится одышка
var holding := false      # сейчас не дышит
var hold_left := 8.0      # запас задержки дыхания
var gasp_lock := false    # после судорожного вдоха надо отпустить Пробел
var filter_left := 120.0
var spares := 0
var suffocating := false
var suffocate_left := 0.0
var cough_timer := 0.0
var changing := 0.0       # идёт замена фильтра
var knock_left := 0.0     # отбрасывание после контакта
var knock_dir := 1.0
var glass_cd := 0.0
var mask_on := true       # в Прологе Артём надевает маску не сразу
var silhouette := false   # true — рисовать силуэтом, а не серым прямоугольником
var walk_phase := 0.0     # для походки
var can_move := true      # уровень может на время забрать управление


func reset_to(px: float, filter: float, spare_count: int) -> void:
	x = px
	filter_left = filter
	spares = spare_count
	mode = "still"
	step_timer = 0.0
	breath_timer = Balance.breath_interval
	run_time = 0.0
	winded = 0.0
	holding = false
	hold_left = Balance.hold_capacity
	gasp_lock = false
	suffocating = false
	changing = 0.0
	knock_left = 0.0
	glass_cd = 0.0


func hold_capacity() -> float:
	return Balance.hold_capacity * (0.5 if winded > 0.0 else 1.0)


func _process(delta: float) -> void:
	if main == null or main.paused:
		return
	var B := Balance
	var near: float = main.nearest_symbiote_distance(x)

	# --- замена фильтра: стоим на месте ---
	if changing > 0.0:
		mode = "change"
		changing -= delta
		if changing <= 0.0:
			filter_left = B.filter_full
			spares -= 1
			suffocating = false
			main.make_noise(x, B.noise_filter_click, "щелчок фильтра")
		_breathe(delta)
		_drain(delta, 1.0)
		_update_position()
		return

	# --- отбрасывание после контакта ---
	if knock_left > 0.0:
		var k := minf(delta, knock_left)
		x = main.clamp_x(x + knock_dir * 7.0 * k)
		knock_left -= k

	# --- «Замри»: Пробел ---
	var freeze_pressed := Input.is_action_pressed("freeze")
	if not freeze_pressed:
		if holding:
			_release_breath()
		gasp_lock = false
	var want_hold := freeze_pressed and not gasp_lock
	if want_hold and not holding and hold_left > 0.0:
		holding = true
	if holding:
		var rate := B.hold_fear_mult if near <= B.hold_fear_distance else 1.0
		hold_left -= rate * delta
		if hold_left <= 0.0:
			hold_left = 0.0
			holding = false
			gasp_lock = true
			breath_timer = B.breath_interval
			main.make_noise(x, B.noise_gasp, "судорожный вдох")
	else:
		var cap := hold_capacity()
		hold_left = minf(cap, hold_left + cap / B.hold_recover_time * delta)

	# --- движение ---
	var dir := Input.get_axis("left", "right") if can_move else 0.0
	var moving := dir != 0.0 and not freeze_pressed and knock_left <= 0.0
	if moving:
		facing = 1 if dir > 0.0 else -1
		if Input.is_action_pressed("run") and not suffocating:
			mode = "run"
		elif Input.is_action_pressed("sneak"):
			mode = "sneak"
		else:
			mode = "walk"
		var speed: float = {"sneak": B.speed_sneak, "walk": B.speed_walk, "run": B.speed_run}[mode]
		if suffocating:
			speed *= 0.5
		x = main.clamp_x(x + dir * speed * delta)
		walk_phase += speed * delta * 2.2
		step_timer -= delta
		if step_timer <= 0.0:
			var r: float = {"sneak": B.noise_sneak, "walk": B.noise_walk, "run": B.noise_run}[mode]
			var label: String = {"sneak": "шаг крадучись", "walk": "шаг", "run": "бег"}[mode]
			main.make_noise(x, r, label)
			if main.has_method("play_step"):
				main.play_step(mode)
			step_timer = {"sneak": B.step_sneak, "walk": B.step_walk, "run": B.step_run}[mode]
		# стекло хрустит, если идти не крадучись
		glass_cd -= delta
		if main.on_glass(x) and mode != "sneak" and glass_cd <= 0.0:
			main.make_noise(x, B.noise_glass, "стекло под ногой")
			glass_cd = 0.8
	else:
		mode = "freeze" if freeze_pressed else "still"
		step_timer = 0.05

	# --- одышка после бега ---
	if mode == "run":
		run_time += delta
		if run_time >= B.winded_after_run:
			winded = B.winded_duration
	else:
		run_time = 0.0
		winded = maxf(0.0, winded - delta)

	# --- действия ---
	if Input.is_action_just_pressed("use"):
		main.try_pickup(self)
	if Input.is_action_just_pressed("change_filter") and spares > 0 and not holding:
		changing = B.filter_change_time

	_breathe(delta)
	var mult := 1.0
	if mode == "run":
		mult = B.drain_run
	elif winded > 0.0:
		mult = B.drain_heavy
	if mask_on:
		_drain(delta, mult)
	elif main.has_method("in_fog") and main.in_fog(x):
		_suffocate(delta)
	else:
		suffocating = false
	_update_position()


func _release_breath() -> void:
	holding = false
	breath_timer = Balance.breath_interval
	if hold_left > hold_capacity() * Balance.hold_quiet_part:
		main.make_noise(x, Balance.noise_exhale_quiet, "тихий выдох")
	else:
		main.make_noise(x, Balance.noise_exhale_loud, "шумный выдох")


func _breathe(delta: float) -> void:
	if holding:
		return
	breath_timer -= delta
	if breath_timer <= 0.0:
		breath_timer = Balance.breath_interval
		if winded > 0.0:
			main.make_noise(x, Balance.noise_breath_heavy, "частое дыхание")
		else:
			main.make_noise(x, Balance.noise_breath_calm, "дыхание")


func _drain(delta: float, mult: float) -> void:
	filter_left = maxf(0.0, filter_left - delta * mult)
	if filter_left <= 0.0:
		_suffocate(delta)


func _suffocate(delta: float) -> void:
	if true:
		if not suffocating:
			suffocating = true
			suffocate_left = Balance.suffocation_time
			cough_timer = 0.0
		suffocate_left -= delta
		cough_timer -= delta
		if cough_timer <= 0.0:
			cough_timer = 3.0
			main.make_noise(x, Balance.noise_cough, "кашель")
		if suffocate_left <= 0.0:
			main.fail("Кончился воздух.")


func on_contact(from_x: float) -> void:
	if Balance.contact_mode == "fail":
		main.fail("Рой коснулся маски.")
		return
	filter_left = maxf(0.0, filter_left - Balance.contact_filter_cost)
	knock_dir = 1.0 if x >= from_x else -1.0
	knock_left = 0.3
	holding = false
	gasp_lock = true
	main.make_noise(x, Balance.noise_cough, "кашель")
	main.flash_message("Симбиот облепил маску: минус %d с фильтра" % int(Balance.contact_filter_cost))


func _update_position() -> void:
	position = Vector2(x * Balance.PX_PER_M, 0.0)
	queue_redraw()


func _draw() -> void:
	if silhouette:
		_draw_silhouette()
		return
	var m := Balance.PX_PER_M
	var h := 1.7 * m
	if mode == "sneak":
		h = 1.3 * m
	var w := 0.5 * m
	var body := Color(0.55, 0.57, 0.6)
	if suffocating:
		body = Color(0.6, 0.45, 0.45)
	draw_rect(Rect2(-w / 2.0, -h, w, h), body)
	# маска — кружок на месте головы, со стороны взгляда
	draw_circle(Vector2(facing * w * 0.3, -h + 0.2 * m), 0.12 * m, Color(0.2, 0.22, 0.24))
	if holding:
		draw_rect(Rect2(-w / 2.0 - 3, -h - 3, w + 6, h + 6), Color(0.4, 0.9, 0.9, 0.9), false, 2.0)
	if changing > 0.0:
		var p := 1.0 - changing / Balance.filter_change_time
		draw_rect(Rect2(-w, -h - 20, 2 * w * p, 6), Color(0.8, 0.8, 0.8))


## Силуэт Артёма: подросток в мешковатой одежде, с противогазом и фильтром.
func _draw_silhouette() -> void:
	var m := Balance.PX_PER_M
	var ink := Color(0.03, 0.03, 0.035)
	var crouch := 0.82 if mode == "sneak" else 1.0
	var moving := mode in ["walk", "run", "sneak"]
	var amp := 0.0
	if moving:
		amp = 0.35 if mode == "run" else 0.22
	var sw := sin(walk_phase) * amp
	var bob := absf(sin(walk_phase)) * (0.04 * m if moving else 0.0)
	# дыхание: плечи чуть поднимаются
	var br := sin(Time.get_ticks_msec() / 1000.0 * TAU / (1.6 if winded > 0.0 else 3.2)) * (0.0 if holding else 0.012 * m)
	var hip := Vector2(0, -0.85 * m * crouch - bob)
	var f := float(facing)
	# ноги
	for side in [-1.0, 1.0]:
		var a: float = sw * side
		var knee := hip + Vector2(sin(a) * 0.42 * m * f, 0.42 * m * crouch)
		var foot := knee + Vector2(sin(a - absf(a) * 0.6) * 0.42 * m * f, 0.42 * m * crouch)
		foot.y = minf(foot.y, 0.0)
		draw_line(hip, knee, ink, 0.17 * m)
		draw_line(knee, foot, ink, 0.15 * m)
		draw_line(foot, foot + Vector2(0.14 * m * f, 0), ink, 0.1 * m)
	# мешковатая куртка
	var sh := hip + Vector2(0.04 * m * f, -0.62 * m * crouch + br)
	var coat := PackedVector2Array([
		hip + Vector2(-0.24 * m, 0.12 * m), hip + Vector2(0.24 * m, 0.12 * m),
		sh + Vector2(0.21 * m, 0.0), sh + Vector2(0.12 * m, -0.06 * m),
		sh + Vector2(-0.14 * m, -0.06 * m), sh + Vector2(-0.22 * m, 0.02 * m)])
	draw_colored_polygon(coat, ink)
	# рука (качается, или держит Дефлотатор)
	var hand := sh + Vector2(sin(-sw) * 0.3 * m * f, 0.55 * m)
	if main != null and main.has_method("aim_point") and main.has_deflotator():
		hand = sh + (main.aim_point() - (position + sh)).normalized() * 0.5 * m
	draw_line(sh + Vector2(0.05 * m * f, 0.05 * m), hand, ink, 0.12 * m)
	# голова и противогаз
	var head := sh + Vector2(0.03 * m * f, -0.2 * m)
	draw_circle(head, 0.15 * m, ink)
	if mask_on:
		draw_circle(head + Vector2(0.11 * m * f, 0.07 * m), 0.08 * m, ink)        # фильтр-коробка
		draw_line(head + Vector2(0.1 * m * f, 0.08 * m), head + Vector2(0.24 * m * f, 0.16 * m), ink, 0.09 * m)
		# стёкла маски едва отсвечивают
		draw_circle(head + Vector2(0.08 * m * f, -0.03 * m), 0.035 * m, Color(0.35, 0.55, 0.55, 0.55))
	if holding:
		draw_arc(head, 0.24 * m, 0, TAU, 24, Color(0.4, 0.9, 0.9, 0.35), 1.5)
	if changing > 0.0:
		var p := 1.0 - changing / Balance.filter_change_time
		draw_rect(Rect2(-0.4 * m, -2.1 * m, 0.8 * m * p, 4), Color(0.8, 0.8, 0.8, 0.8))
