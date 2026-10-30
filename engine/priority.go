package union

// EasyMount overlay for rclone v1.75.2 (MIT). New-file placement only:
// configured order, first source able to hold the entire file. Existing files
// stay on their original source and retain upstream update semantics.
import (
	"context"
	"encoding/json"
	"fmt"
	"io"
	"os"
	"strconv"
	"strings"
	"sync/atomic"

	"github.com/rclone/rclone/backend/union/common"
	"github.com/rclone/rclone/fs"
	"github.com/rclone/rclone/fs/walk"
)

func applyEasyMountPlan(opt *common.Options) error {
	if opt.EasyMountPlan == "" { return nil }
	data, err := os.ReadFile(opt.EasyMountPlan)
	if err != nil { return fmt.Errorf("read EasyMount pool plan: %w", err) }
	var sources []struct { Remote string; LimitBytes int64 }
	if err = json.Unmarshal(data, &sources); err != nil { return fmt.Errorf("invalid EasyMount pool plan: %w", err) }
	if len(sources) < 2 { return fmt.Errorf("EasyMount pool needs at least two sources") }
	opt.Upstreams = nil
	limits := make([]string, len(sources))
	for i, source := range sources {
		if source.Remote == "" || source.LimitBytes < 0 { return fmt.Errorf("invalid EasyMount source %d", i+1) }
		opt.Upstreams = append(opt.Upstreams, source.Remote)
		limits[i] = strconv.FormatInt(source.LimitBytes, 10)
	}
	opt.EasyMountLimits = strings.Join(limits, ",")
	opt.EasyMountPriority = true
	opt.CreatePolicy, opt.ActionPolicy, opt.SearchPolicy = "ff", "epall", "epall"
	return nil
}

func (f *Fs) priorityPut(ctx context.Context, in io.Reader, src fs.ObjectInfo, options ...fs.OpenOption) (fs.Object, error) {
	if src.Size() < 0 {
		return nil, fmt.Errorf("EasyMount priority requires a known file size; enable VFS write caching")
	}
	// ponytail: serialize new-file placement per pool; use quota reservations if
	// parallel uploads are needed. No unbounded buffer or second spool file.
	f.priorityMu.Lock()
	defer f.priorityMu.Unlock()
	limits := strings.Split(f.opt.EasyMountLimits, ",")
	if f.opt.EasyMountLimits != "" && len(limits) != len(f.upstreams) {
		return nil, fmt.Errorf("EasyMount quota limits must match the upstream count")
	}
	for i, u := range f.upstreams {
		if err := ctx.Err(); err != nil { return nil, err }
		if !u.IsCreatable() { continue }
		var limit int64
		if f.opt.EasyMountLimits != "" {
			var err error
			limit, err = strconv.ParseInt(limits[i], 10, 64)
			if err != nil || limit < 0 { return nil, fmt.Errorf("invalid EasyMount quota limit at source %d", i+1) }
		}
		free := int64(-1)
		if about := u.RootFs.Features().About; about != nil {
			usage, err := about(ctx)
			if err != nil { return nil, fmt.Errorf("source %d quota check: %w", i+1, err) }
			if usage != nil && usage.Free != nil { free = *usage.Free }
		}
		if limit > 0 {
			var used atomic.Int64
			// Count all files, regardless of a copy command's include/exclude filters.
			err := walk.ListR(ctx, u.RootFs, "", true, -1, walk.ListObjects, func(entries fs.DirEntries) error {
				for _, entry := range entries {
					if entry.Size() < 0 || used.Add(entry.Size()) < 0 { return fmt.Errorf("unknown or overflowing object size") }
				}
				return nil
			})
			if err == fs.ErrorDirNotFound { err = nil }
			if err != nil { return nil, fmt.Errorf("source %d: cannot measure folder usage for its quota limit: %w", i+1, err) }
			remaining := limit - used.Load()
			if remaining < 0 { remaining = 0 }
			if free < 0 || remaining < free { free = remaining }
		}
		if free < 0 {
			return nil, fmt.Errorf("source %d does not report free space; set its EasyMount folder limit before writing", i+1)
		}
		if free < src.Size() || (src.Size() == 0 && free == 0) { continue }
		if err := u.Mkdir(ctx, parentDir(src.Remote())); err != nil { return nil, err }
		o, err := u.Put(ctx, in, src, options...)
		if err != nil { return nil, err } // Preserve the VFS cache on API/network/quota errors.
		entry, err := f.wrapEntries(u.WrapObject(o))
		if err != nil { return nil, err }
		return entry.(*Object), nil
	}
	return nil, fmt.Errorf("EasyMount: no source has enough free space for this file (%d bytes); cached upload is retained", src.Size())
}
