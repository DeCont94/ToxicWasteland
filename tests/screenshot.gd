extends SceneTree
## Снимок экрана для проверки картинки (нужен экран, не --headless):
## godot --path . -s res://tests/screenshot.gd
var main
var f := 0
func _initialize() -> void:
	main = load("res://scenes/main.tscn").instantiate()
	root.add_child(main)
func _process(_d: float) -> bool:
	f += 1
	if f == 2:
		main.debug = true
	if f < 330:
		Input.action_press("right")
	else:
		Input.action_release("right")
	if f == 330:
		root.get_viewport().get_texture().get_image().save_png("user://shot.png")
		print(ProjectSettings.globalize_path("user://shot.png"))
		return true
	return false
