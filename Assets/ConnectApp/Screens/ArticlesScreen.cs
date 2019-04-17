using System;
using System.Collections.Generic;
using ConnectApp.canvas;
using ConnectApp.components;
using ConnectApp.components.pull_to_refresh;
using ConnectApp.constants;
using ConnectApp.models;
using ConnectApp.Models.ActionModel;
using ConnectApp.Models.ViewModel;
using ConnectApp.redux.actions;
using RSG;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.Redux;
using Unity.UIWidgets.scheduler;
using Unity.UIWidgets.widgets;
using Color = Unity.UIWidgets.ui.Color;
using Icons = ConnectApp.constants.Icons;

namespace ConnectApp.screens {
    public class ArticlesScreenConnector : StatelessWidget {
        public override Widget build(BuildContext context) {
            return new StoreConnector<AppState, ArticlesScreenViewModel>(
                converter: state => new ArticlesScreenViewModel {
                    articlesLoading = state.articleState.articlesLoading,
                    articleList = state.articleState.articleList,
                    articleDict = state.articleState.articleDict,
                    hottestHasMore = state.articleState.hottestHasMore,
                    userDict = state.userState.userDict,
                    teamDict = state.teamState.teamDict,
                    isLoggedIn = state.loginState.isLoggedIn,
                    hosttestOffset = state.articleState.articleList.Count
                },
                builder: (context1, viewModel, dispatcher) => {
                    var actionModel = new ArticlesScreenActionModel {
                        pushToSearch = () => dispatcher.dispatch(new MainNavigatorPushToAction {
                            routeName = MainNavigatorRoutes.Search
                        }),
                        pushToLogin = () => dispatcher.dispatch(new MainNavigatorPushToAction {
                            routeName = MainNavigatorRoutes.Login
                        }),
                        pushToArticleDetail = id => dispatcher.dispatch(
                            new MainNavigatorPushToArticleDetailAction {
                                articleId = id
                            }
                        ),
                        pushToReport = (reportId, reportType) => dispatcher.dispatch(
                            new MainNavigatorPushToReportAction {
                                reportId = reportId,
                                reportType = reportType
                            }
                        ),
                        startFetchArticles = () => dispatcher.dispatch(new StartFetchArticlesAction()),
                        fetchArticles = offset => dispatcher.dispatch<IPromise>(Actions.fetchArticles(offset))
                    };
                    return new ArticlesScreen(viewModel, actionModel);
                });
        }
    }

    public class ArticlesScreen : StatefulWidget {
        public override State createState() {
            return new _ArticlesScreenState();
        }

        public ArticlesScreen(
            ArticlesScreenViewModel viewModel = null,
            ArticlesScreenActionModel actionModel = null,
            Key key = null
        ) : base(key) {
            this.viewModel = viewModel;
            this.actionModel = actionModel;
        }

        public readonly ArticlesScreenViewModel viewModel;
        public readonly ArticlesScreenActionModel actionModel;
    }


    public class _ArticlesScreenState : State<ArticlesScreen> {
        private const int initOffset = 0;
        private int offset = initOffset;
        private RefreshController _refreshController;
        private TextStyle titleStyle;
        const float maxNavBarHeight = 96; 
        const float minNavBarHeight = 44; 
        private float navBarHeight;

        public override void initState() {
            base.initState();
            _refreshController = new RefreshController();
            navBarHeight = maxNavBarHeight;
            titleStyle = CTextStyle.H2;
            SchedulerBinding.instance.addPostFrameCallback(_ => {
                widget.actionModel.startFetchArticles();
                widget.actionModel.fetchArticles(initOffset);
            });
        }

        public override Widget build(BuildContext context) {
            return new Container(
                color: CColors.BgGrey,
                child: new Column(
                    children: new List<Widget> {
                        _buildNavigationBar(),
                        new Flexible(
                            child: _buildArticleList()
                        )
                    }
                )
            );
        }

        private Widget _buildNavigationBar() {
            return new AnimatedContainer(
                height: navBarHeight,
                color: CColors.White,
                duration: new TimeSpan(0, 0, 0, 0, 0),
                child: new Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: new List<Widget> {
                        new Container(
                            padding: EdgeInsets.only(16, bottom: 8),
                            child: new AnimatedDefaultTextStyle(
                                child: new Text("文章"),
                                style: titleStyle, 
                                duration: new TimeSpan(0, 0, 0, 0, 100)
                            )
                        ),
                        new CustomButton(
                            padding: EdgeInsets.only(16, 8, 16, 8),
                            onPressed: () => widget.actionModel.pushToSearch(),
                            child: new Icon(
                                Icons.search,
                                size: 28,
                                color: Color.fromRGBO(181, 181, 181, 1)
                            )
                        )
                    }
                )   
            );
        }

        private Widget _buildArticleList() {
            Widget content = new Container();

            if (widget.viewModel.articlesLoading && widget.viewModel.articleList.isEmpty())
                content = ListView.builder(
                    itemCount: 4,
                    itemBuilder: (cxt, index) => new ArticleLoading()
                );
            else if (widget.viewModel.articleList.Count <= 0)
                content = new BlankView("暂无文章");
            else
                content = new SmartRefresher(
                    controller: _refreshController,
                    enablePullDown: true,
                    enablePullUp: widget.viewModel.hottestHasMore,
                    onRefresh: onRefresh,
                    child: ListView.builder(
                        physics: new AlwaysScrollableScrollPhysics(),
                        itemCount: widget.viewModel.articleList.Count,
                        itemBuilder: (cxt, index) => {
                            var articleId = widget.viewModel.articleList[index];
                            var article = widget.viewModel.articleDict[articleId];
                            var fullName = "";
                            if (article.ownerType == OwnerType.user.ToString()) {
                                if (widget.viewModel.userDict.ContainsKey(article.userId))
                                    fullName = widget.viewModel.userDict[article.userId].fullName;
                            }
                            if (article.ownerType == OwnerType.team.ToString()) {
                                if (widget.viewModel.teamDict.ContainsKey(article.teamId))
                                    fullName = widget.viewModel.teamDict[article.teamId].name;
                            }
                            return new ArticleCard(
                                article,
                                () => widget.actionModel.pushToArticleDetail(articleId),
                                () => {
                                    if (!widget.viewModel.isLoggedIn) {
                                        widget.actionModel.pushToLogin();
                                        return;
                                    } 
                                    ActionSheetUtils.showModalActionSheet(new ActionSheet(
                                        items: new List<ActionSheetItem> {
                                            new ActionSheetItem(
                                                "举报",
                                                ActionType.normal,
                                                () => widget.actionModel.pushToReport(articleId, ReportType.article)
                                            ),
                                            new ActionSheetItem("取消", ActionType.cancel)
                                        }
                                    ));
                                },
                                fullName,
                                new ObjectKey(article.id)
                            );
                        }
                    )
                );

            return new NotificationListener<ScrollNotification>(
                onNotification: _onNotification,
                child: new Container(
                    margin: EdgeInsets.only(bottom: 49),
                    child: content
                )
            );
        }

        private void onRefresh(bool up) {
            if (up)
                offset = initOffset;
            else
                offset = widget.viewModel.hosttestOffset;
            widget.actionModel.fetchArticles(offset)
                .Then(() => _refreshController.sendBack(up, up ? RefreshStatus.completed : RefreshStatus.idle))
                .Catch(_ => _refreshController.sendBack(up, RefreshStatus.failed));
        }

        private bool _onNotification(ScrollNotification notification) {
            var pixels = notification.metrics.pixels;
            SchedulerBinding.instance.addPostFrameCallback(_ => {
                if (pixels > 0 && pixels <= 52) {
                    titleStyle = CTextStyle.H5;
                    navBarHeight = maxNavBarHeight - pixels;
                    setState(() => {});
                }
                else if (pixels <= 0) {
                    if (navBarHeight <= maxNavBarHeight) {
                        titleStyle = CTextStyle.H2;
                        navBarHeight = maxNavBarHeight;
                        setState(() => {});
                    }
                }
                else if (pixels > 52) {
                    if (!(navBarHeight <= minNavBarHeight)) {
                        titleStyle = CTextStyle.H5;
                        navBarHeight = minNavBarHeight;
                        setState(() => {});
                    }
                }
            });
            return true;
        }
    }
}