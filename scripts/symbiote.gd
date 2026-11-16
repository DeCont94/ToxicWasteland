class_name Symbiote
extends Node2D
## Симбиот: слепой, идёт на шум. Состояния: дрейф → насторожен → (тишина) → рывок.

enum S { DRIFT, ALERT, WINDUP, RUSH }
const STATE_NAMES := ["дрейф", "насторожен", "тишина перед рывком", "рывок"]

var main: Node
var x := 0.0
var patrol_a := 0.0
var patrol_b := 0.0
var dir := 1.0
var state := S.DRIFT
var target := 0.0          # куда идёт / куда рвётся
var quiet_time := 0.0      # сколько тишины с последнего услышанного шума
var last_heard := -100.0   # когда последний раз услышал (время уровня)
var timer := 0.0
var deaf := 0.0
var t := 0.0               # для мерцания
var float_h := 1.1         # высота над землёй, м


func setup(start_x: float, a: float, b: float) -> void:
	x = start_x
	patrol_a = a
	patrol_b = b
	dir = 1.0
	state = S.DRIFT
	deaf = 0.0
	last_heard = -100.0
	_update_position()


## Возвращает true, если симбиот услышал шум.
func hear(nx: float, radius: float, label: String, now: float) -> bool:
	if deaf > 0.0:
		return false
	var r: float = radius * Balance.sym_sensitivity * main.wall_factor_between(x, nx)
	if absf(nx - x) > r:
		return false
	var since := now - last_heard
	if since < Balance.sym_reaction_grace and state == S.ALERT:
		# тот же самый «звук» — просто уточняет направление
		target = nx
		quiet_time = 0.0
		return true
	match state:
		S.DRIFT:
			state = S.ALERT
			target = nx
		S.ALERT:
			if since <= Balance.sym_second_noise:
				_start_rush(nx)
			else:
				target = nx
		S.WINDUP, S.RUSH:
			# в рывке гонится за звуком и проскакивает чуть дальше
			var s := signf(nx - x)
			if s == 0.0:
				s = dir
			target = nx + s * 1.5
			if state == S.RUSH:
				timer = maxf(timer, 1.0)
	quiet_time = 0.0
	last_heard = now
	main.on_heard(self, nx, radius, label)
	return true


func _start_rush(toward: float) -> void:
	state = S.WINDUP
	timer = Balance.sym_rush_windup
	# рвётся к звуку и чуть дальше
	var s := signf(toward - x)
	if s == 0.0:
		s = dir
	target = toward + s * 1.5


func _process(delta: float) -> void:
	if main == null or main.paused:
		return
	t += delta
	deaf = maxf(0.0, deaf - delta)
	var B := Balance
	match state:
		S.DRIFT:
			var lo := minf(patrol_a, patrol_b)
			var hi := maxf(patrol_a, patrol_b)
			if x < lo:
				dir = 1.0
			elif x > hi:
				dir = -1.0
			x += dir * B.sym_speed_drift * delta
			if x >= hi and dir > 0.0:
				dir = -1.0
			elif x <= lo and dir < 0.0:
				dir = 1.0
		S.ALERT:
			quiet_time += delta
			_move_to(target, B.sym_speed_alert, delta)
			if B.sym_find_distance > 0.0 and absf(target - x) < 0.15 and absf(main.player.x - target) <= B.sym_find_distance and deaf <= 0.0:
				_start_rush(main.player.x)
			elif absf(target - x) < 0.15 or quiet_time >= B.sym_alert_forget:
				# дошёл до места звука и ничего не услышал — плывёт дальше (замершего не находят)
				state = S.DRIFT
		S.WINDUP:
			timer -= delta
			if timer <= 0.0:
				state = S.RUSH
				timer = B.sym_rush_time
		S.RUSH:
			timer -= delta
			_move_to(target, B.sym_speed_rush, delta)
			if absf(main.player.x - x) <= B.sym_contact_distance:
				main.on_contact(self)
				state = S.ALERT
				target = x
				quiet_time = 0.0
				deaf = B.sym_deaf_after_contact
				# отплывает от Артёма
				x += 1.5 * signf(x - main.player.x if x != main.player.x else -dir)
			elif timer <= 0.0 or absf(target - x) < 0.1:
				state = S.ALERT
				target = x
				quiet_time = 0.0
	_update_position()


func _move_to(goal: float, speed: float, delta: float) -> void:
	var d := goal - x
	if absf(d) < 0.01:
		return
	var s := signf(d)
	dir = s
	x += s * minf(absf(d), speed * delta)


func _update_position() -> void:
	position = Vector2(x * Balance.PX_PER_M, -float_h * Balance.PX_PER_M)
	queue_redraw()


func _draw() -> void:
	var m := Balance.PX_PER_M
	var core := Color(0.25, 0.75, 0.7)
	var glow_a := 0.12
	match state:
		S.DRIFT:
			core = Color(0.2, 0.6, 0.55)
		S.ALERT:
			var f := 0.5 + 0.5 * sin(t * 18.0)
			core = Color(0.3, 0.9, 0.8).lerp(Color(0.6, 1.0, 0.95), f)
			glow_a = 0.22 + 0.1 * f
		S.WINDUP:
			core = Color(0.08, 0.12, 0.12)
			glow_a = 0.03
		S.RUSH:
			core = Color(0.85, 1.0, 1.0)
			glow_a = 0.35
	draw_circle(Vector2.ZERO, 1.2 * m, Color(core.r, core.g, core.b, glow_a))
	draw_circle(Vector2.ZERO, 0.45 * m, core)
	if main != null and main.debug:
		var font := ThemeDB.fallback_font
		draw_string(font, Vector2(-60, -0.8 * m), STATE_NAMES[state], HORIZONTAL_ALIGNMENT_CENTER, 120, 14, Color(0.8, 0.9, 0.9))
