#!/bin/bash

# Forum.Api 测试运行脚本
# 支持单元测试、集成测试、覆盖率报告生成

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 默认参数
TEST_TYPE="all"
COVERAGE=true
PARALLEL=true
WATCH=false
RESULTS_DIR="./TestResults"
COVERAGE_DIR="./Coverage"

# 帮助信息
show_help() {
    echo "Forum.Api 测试运行脚本"
    echo
    echo "用法: $0 [选项]"
    echo
    echo "选项:"
    echo "  -t, --type TYPE      测试类型: unit|integration|all (默认: all)"
    echo "  -c, --coverage       生成覆盖率报告 (默认: 开启)"
    echo "  --no-coverage        禁用覆盖率报告"
    echo "  -p, --parallel       并行运行测试 (默认: 开启)"
    echo "  --no-parallel        串行运行测试"
    echo "  -w, --watch          监控模式"
    echo "  -v, --verbose        详细输出"
    echo "  -h, --help           显示帮助信息"
    echo
    echo "示例:"
    echo "  $0                   # 运行所有测试并生成覆盖率报告"
    echo "  $0 -t unit           # 只运行单元测试"
    echo "  $0 -t integration    # 只运行集成测试"
    echo "  $0 --no-coverage     # 运行测试但不生成覆盖率报告"
    echo "  $0 -w                # 监控模式运行"
    echo
}

# 日志函数
log_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

log_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

log_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

log_error() {
    echo -e "${RED}❌ $1${NC}"
}

# 检查依赖
check_dependencies() {
    log_info "检查依赖..."
    
    # 检查 .NET
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET SDK 未找到，请安装 .NET 8 SDK"
        exit 1
    fi
    
    # 检查 Docker（用于集成测试）
    if [[ "$TEST_TYPE" == "integration" || "$TEST_TYPE" == "all" ]]; then
        if ! command -v docker &> /dev/null; then
            log_warning "Docker 未找到，集成测试将跳过"
        fi
    fi
    
    log_success "依赖检查完成"
}

# 清理之前的结果
cleanup() {
    log_info "清理之前的测试结果..."
    rm -rf "$RESULTS_DIR"
    rm -rf "$COVERAGE_DIR"
    mkdir -p "$RESULTS_DIR"
    mkdir -p "$COVERAGE_DIR"
}

# 构建解决方案
build_solution() {
    log_info "构建解决方案..."
    dotnet restore
    dotnet build --configuration Release --no-restore
    log_success "构建完成"
}

# 运行单元测试
run_unit_tests() {
    log_info "运行单元测试..."
    
    local test_args=(
        "Forum.Api.Tests/Forum.Api.Tests.csproj"
        "--configuration" "Release"
        "--no-build"
        "--filter" "Category!=Integration"
        "--results-directory" "$RESULTS_DIR/Unit"
        "--logger" "trx"
        "--logger" "console;verbosity=normal"
    )
    
    if [[ "$COVERAGE" == true ]]; then
        test_args+=(
            "--collect" "XPlat Code Coverage"
            "--settings" "Forum.Api.Tests/coverlet.runsettings"
        )
    fi
    
    if [[ "$PARALLEL" == true ]]; then
        test_args+=("--parallel")
    fi
    
    if [[ "$WATCH" == true ]]; then
        dotnet watch test "${test_args[@]}"
    else
        dotnet test "${test_args[@]}"
    fi
    
    log_success "单元测试完成"
}

# 启动测试数据库
start_test_database() {
    log_info "启动测试数据库..."
    
    # 检查是否已有运行的测试数据库容器
    if docker ps -q -f name=forum_test_db &> /dev/null; then
        log_info "测试数据库已在运行"
        return
    fi
    
    # 启动 MySQL 容器
    docker run -d \
        --name forum_test_db \
        --rm \
        -e MYSQL_ROOT_PASSWORD=password \
        -e MYSQL_DATABASE=forum_test \
        -p 3306:3306 \
        mysql:8.0 \
        --default-authentication-plugin=mysql_native_password
    
    # 等待数据库启动
    log_info "等待数据库启动..."
    for i in {1..30}; do
        if docker exec forum_test_db mysqladmin ping -uroot -ppassword --silent; then
            log_success "数据库启动完成"
            return
        fi
        sleep 2
    done
    
    log_error "数据库启动超时"
    exit 1
}

# 停止测试数据库
stop_test_database() {
    log_info "停止测试数据库..."
    if docker ps -q -f name=forum_test_db &> /dev/null; then
        docker stop forum_test_db
        log_success "数据库已停止"
    fi
}

# 运行集成测试
run_integration_tests() {
    log_info "运行集成测试..."
    
    start_test_database
    
    local test_args=(
        "Forum.Api.Tests/Forum.Api.Tests.csproj"
        "--configuration" "Release"
        "--no-build"
        "--filter" "Category=Integration"
        "--results-directory" "$RESULTS_DIR/Integration"
        "--logger" "trx"
        "--logger" "console;verbosity=normal"
    )
    
    if [[ "$COVERAGE" == true ]]; then
        test_args+=(
            "--collect" "XPlat Code Coverage"
            "--settings" "Forum.Api.Tests/coverlet.runsettings"
        )
    fi
    
    # 设置环境变量
    export ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=forum_test;Uid=root;Pwd=password;CharSet=utf8mb4;"
    
    if [[ "$WATCH" == true ]]; then
        dotnet watch test "${test_args[@]}"
    else
        dotnet test "${test_args[@]}"
    fi
    
    stop_test_database
    log_success "集成测试完成"
}

# 生成覆盖率报告
generate_coverage_report() {
    if [[ "$COVERAGE" != true ]]; then
        return
    fi
    
    log_info "生成覆盖率报告..."
    
    # 检查是否安装了 ReportGenerator
    if ! dotnet tool list -g | grep -q reportgenerator; then
        log_info "安装 ReportGenerator..."
        dotnet tool install -g dotnet-reportgenerator-globaltool
    fi
    
    # 生成报告
    reportgenerator \
        -reports:"$RESULTS_DIR/**/*.xml" \
        -targetdir:"$COVERAGE_DIR/Report" \
        -reporttypes:"Html;Cobertura;JsonSummary;TextSummary" \
        -verbosity:"Info"
    
    # 显示覆盖率摘要
    if [[ -f "$COVERAGE_DIR/Report/Summary.txt" ]]; then
        echo
        log_info "覆盖率摘要:"
        cat "$COVERAGE_DIR/Report/Summary.txt"
        echo
    fi
    
    # 检查覆盖率阈值
    if [[ -f "$COVERAGE_DIR/Report/Summary.json" ]] && command -v jq &> /dev/null; then
        local line_coverage
        line_coverage=$(jq -r '.summary.linecoverage' "$COVERAGE_DIR/Report/Summary.json")
        
        echo "总体行覆盖率: ${line_coverage}%"
        
        if (( $(echo "$line_coverage >= 80" | bc -l) )); then
            log_success "覆盖率达标: ${line_coverage}% >= 80%"
        else
            log_warning "覆盖率未达标: ${line_coverage}% < 80%"
        fi
    fi
    
    log_success "覆盖率报告生成完成"
    log_info "报告位置: $COVERAGE_DIR/Report/index.html"
}

# 解析命令行参数
parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -t|--type)
                TEST_TYPE="$2"
                shift 2
                ;;
            -c|--coverage)
                COVERAGE=true
                shift
                ;;
            --no-coverage)
                COVERAGE=false
                shift
                ;;
            -p|--parallel)
                PARALLEL=true
                shift
                ;;
            --no-parallel)
                PARALLEL=false
                shift
                ;;
            -w|--watch)
                WATCH=true
                shift
                ;;
            -v|--verbose)
                VERBOSE=true
                shift
                ;;
            -h|--help)
                show_help
                exit 0
                ;;
            *)
                log_error "未知参数: $1"
                show_help
                exit 1
                ;;
        esac
    done
}

# 主函数
main() {
    parse_arguments "$@"
    
    log_info "Forum.Api 测试运行器"
    log_info "测试类型: $TEST_TYPE"
    log_info "覆盖率报告: $COVERAGE"
    log_info "并行运行: $PARALLEL"
    echo
    
    check_dependencies
    cleanup
    build_solution
    
    case "$TEST_TYPE" in
        "unit")
            run_unit_tests
            ;;
        "integration")
            run_integration_tests
            ;;
        "all")
            run_unit_tests
            run_integration_tests
            ;;
        *)
            log_error "无效的测试类型: $TEST_TYPE"
            show_help
            exit 1
            ;;
    esac
    
    generate_coverage_report
    
    log_success "所有测试完成！"
}

# 错误处理
trap 'log_error "脚本执行失败"; stop_test_database; exit 1' ERR

# 运行主函数
main "$@"