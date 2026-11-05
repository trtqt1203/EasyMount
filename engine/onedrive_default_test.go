package onedrive

import (
	"context"
	"fmt"
	"net/http"
	"net/http/httptest"
	"testing"

	"github.com/rclone/rclone/fs/config/configmap"
	"github.com/rclone/rclone/lib/rest"
)

// Graph can enumerate internal personal-drive containers before the usable
// /me/drive. The default choice must be the canonical user drive, not item zero.
func TestEasyMountDefaultDrive(t *testing.T) {
	for _, scenario := range []struct{ name, list, primary, first string }{
		{"already present", `{"value":[{"id":"internal"},{"id":"primary"},{"id":"shared"}]}`, `{"id":"primary","name":"OneDrive","driveType":"personal"}`, "primary"},
		{"missing from list", `{"value":[{"id":"shared"}]}`, `{"id":"primary","name":"OneDrive","driveType":"personal"}`, "primary"},
		{"default unavailable", `{"value":[{"id":"shared"}]}`, "", "shared"},
	} {
		t.Run(scenario.name, func(t *testing.T) {
			server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
				w.Header().Set("Content-Type", "application/json")
				if r.URL.Path == "/me/drives" { fmt.Fprint(w, scenario.list); return }
				if r.URL.Path == "/me/drive" && scenario.primary != "" { fmt.Fprint(w, scenario.primary); return }
				http.Error(w, `{"error":{"code":"notFound"}}`, http.StatusNotFound)
			}))
			defer server.Close()
			step, err := chooseDrive(context.Background(), "fixture", configmap.Simple{}, rest.NewClient(server.Client()), chooseDriveOpt{opts:rest.Opts{Method:"GET", RootURL:server.URL, Path:"/me/drives"}})
			if err != nil { t.Fatal(err) }
			if step == nil || step.Option == nil || len(step.Option.Examples) == 0 { t.Fatal("missing drive choices") }
			if got := fmt.Sprint(step.Option.Examples[0].Value); got != scenario.first { t.Fatalf("first choice = %s, want canonical %s", got, scenario.first) }
			seen := map[string]bool{}
			for _, choice := range step.Option.Examples { if seen[choice.Value] { t.Fatal("duplicate drive choice") }; seen[choice.Value] = true }
		})
	}
}
