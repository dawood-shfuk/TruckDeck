"""Admin panel routes."""
from flask import (
    Blueprint,
    abort,
    flash,
    redirect,
    render_template,
    request,
    url_for,
)

from auth import (
    MIN_PASSWORD_LEN,
    clear_login_attempts,
    create_admin,
    issue_csrf_token,
    login_admin,
    login_rate_limited,
    login_required,
    logout_admin,
    record_failed_login,
    registration_allowed,
    require_registration_open,
    update_password,
    validate_csrf,
    verify_login,
)

admin_bp = Blueprint("admin", __name__, url_prefix="/admin")


@admin_bp.after_request
def admin_no_cache(response):
    response.headers["Cache-Control"] = "no-store"
    response.headers["Pragma"] = "no-cache"
    return response


@admin_bp.route("/")
def admin_index():
    from auth import is_logged_in, registration_allowed

    if is_logged_in():
        return redirect(url_for("admin.dashboard"))
    if registration_allowed():
        return redirect(url_for("admin.register"))
    return redirect(url_for("admin.login"))


@admin_bp.route("/dashboard")
@login_required
def dashboard():
    from app import APP_VERSION, download_meta, get_counts
    from traffic import get_traffic_stats, referer_rows

    traffic = get_traffic_stats()
    return render_template(
        "admin/dashboard.html",
        version=APP_VERSION,
        downloads=download_meta(),
        counts=get_counts(),
        traffic=traffic,
        visitor_referers=referer_rows(traffic.get("visitors", {})),
        bot_referers=referer_rows(traffic.get("bots", {})),
        csrf_token=issue_csrf_token(),
    )


@admin_bp.route("/register", methods=["GET", "POST"])
def register():
    require_registration_open()

    from app import APP_VERSION

    if request.method == "POST":
        if not validate_csrf(request.form.get("csrf_token")):
            flash("Invalid form token. Refresh and try again.", "error")
            return render_template(
                "admin/register.html",
                version=APP_VERSION,
                csrf_token=issue_csrf_token(),
                min_password=MIN_PASSWORD_LEN,
            ), 400

        username = request.form.get("username", "")
        password = request.form.get("password", "")
        confirm = request.form.get("confirm_password", "")

        if password != confirm:
            flash("Passwords do not match.", "error")
        else:
            try:
                create_admin(username, password)
                login_admin(username.strip())
                flash("Admin account created. Welcome aboard, dispatcher.", "success")
                return redirect(url_for("admin.dashboard"))
            except ValueError as exc:
                flash(str(exc), "error")

    return render_template(
        "admin/register.html",
        version=APP_VERSION,
        csrf_token=issue_csrf_token(),
        min_password=MIN_PASSWORD_LEN,
    )


@admin_bp.route("/login", methods=["GET", "POST"])
def login():
    from app import APP_VERSION
    from auth import is_logged_in

    if is_logged_in():
        return redirect(url_for("admin.dashboard"))

    if not registration_allowed():
        if request.method == "GET":
            pass
        elif request.method == "POST":
            pass
    else:
        return redirect(url_for("admin.register"))

    if request.method == "POST":
        if login_rate_limited():
            flash("Too many failed attempts. Try again in 15 minutes.", "error")
            return render_template(
                "admin/login.html",
                version=APP_VERSION,
                csrf_token=issue_csrf_token(),
            ), 429

        if not validate_csrf(request.form.get("csrf_token")):
            flash("Invalid form token. Refresh and try again.", "error")
            return render_template(
                "admin/login.html",
                version=APP_VERSION,
                csrf_token=issue_csrf_token(),
            ), 400

        username = request.form.get("username", "")
        password = request.form.get("password", "")

        if verify_login(username, password):
            clear_login_attempts()
            login_admin(username.strip())
            next_url = request.args.get("next")
            if next_url and next_url.startswith("/admin"):
                return redirect(next_url)
            return redirect(url_for("admin.dashboard"))

        record_failed_login()
        flash("Invalid username or password.", "error")

    return render_template(
        "admin/login.html",
        version=APP_VERSION,
        csrf_token=issue_csrf_token(),
    )


@admin_bp.route("/logout", methods=["POST"])
@login_required
def logout():
    if not validate_csrf(request.form.get("csrf_token")):
        abort(400)
    logout_admin()
    flash("Logged out.", "success")
    return redirect(url_for("admin.login"))


@admin_bp.route("/counts/reset", methods=["POST"])
@login_required
def reset_counts():
    if not validate_csrf(request.form.get("csrf_token")):
        abort(400)

    from app import COUNTS_FILE

    COUNTS_FILE.write_text("{}", encoding="utf-8")
    flash("Download counters cleared.", "success")
    return redirect(url_for("admin.dashboard"))


@admin_bp.route("/traffic/reset", methods=["POST"])
@login_required
def reset_traffic_counts():
    if not validate_csrf(request.form.get("csrf_token")):
        abort(400)

    from traffic import reset_traffic

    reset_traffic()
    flash("Visitor and bot counters cleared.", "success")
    return redirect(url_for("admin.dashboard"))


@admin_bp.route("/password", methods=["POST"])
@login_required
def change_password():
    if not validate_csrf(request.form.get("csrf_token")):
        abort(400)

    current = request.form.get("current_password", "")
    new_pass = request.form.get("new_password", "")
    confirm = request.form.get("confirm_password", "")

    if new_pass != confirm:
        flash("New passwords do not match.", "error")
    else:
        try:
            update_password(current, new_pass)
            flash("Password updated.", "success")
        except ValueError as exc:
            flash(str(exc), "error")

    return redirect(url_for("admin.dashboard"))


@admin_bp.route("/reviews")
@login_required
def admin_reviews():
    from app import APP_VERSION
    from reviews_bp import list_all_reviews, list_pending_reviews

    return render_template(
        "admin/reviews.html",
        version=APP_VERSION,
        pending=list_pending_reviews(),
        recent=list_all_reviews(30),
        csrf_token=issue_csrf_token(),
    )


@admin_bp.route("/reviews/<int:review_id>/approve", methods=["POST"])
@login_required
def approve_review(review_id: int):
    if not validate_csrf(request.form.get("csrf_token")):
        abort(400)
    from reviews_bp import moderate_review

    if moderate_review(review_id, True):
        flash("Review approved.", "success")
    else:
        flash("Review not found or already moderated.", "error")
    return redirect(url_for("admin.admin_reviews"))


@admin_bp.route("/reviews/<int:review_id>/reject", methods=["POST"])
@login_required
def reject_review(review_id: int):
    if not validate_csrf(request.form.get("csrf_token")):
        abort(400)
    from reviews_bp import moderate_review

    if moderate_review(review_id, False):
        flash("Review rejected.", "success")
    else:
        flash("Review not found or already moderated.", "error")
    return redirect(url_for("admin.admin_reviews"))


@admin_bp.route("/crashes")
@login_required
def admin_crashes():
    from app import APP_VERSION
    from crash_bp import list_crash_reports

    return render_template(
        "admin/crashes.html",
        version=APP_VERSION,
        reports=list_crash_reports(50),
        csrf_token=issue_csrf_token(),
    )


@admin_bp.route("/crashes/<int:report_id>")
@login_required
def admin_crash_detail(report_id: int):
    from app import APP_VERSION
    from crash_bp import get_crash_report

    report = get_crash_report(report_id)
    if not report:
        abort(404)
    return render_template(
        "admin/crash_detail.html",
        version=APP_VERSION,
        report=report,
        csrf_token=issue_csrf_token(),
    )
